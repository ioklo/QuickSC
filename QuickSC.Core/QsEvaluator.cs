using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Syntax;
using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;

namespace QuickSC
{
    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        private QsAnalyzer analyzer;
        private QsExpEvaluator expValueEvaluator;
        private QsStmtEvaluator stmtEvaluator;        

        public QsEvaluator(QsAnalyzer analyzer, IQsCommandProvider commandProvider)
        {
            this.analyzer = analyzer;
            this.expValueEvaluator = new QsExpEvaluator(this);
            this.stmtEvaluator = new QsStmtEvaluator(this, commandProvider);
        }        
        
        public ValueTask EvaluateStringExpAsync(StringExp command, QsValue result, QsEvalContext context)
        {
            return expValueEvaluator.EvalStringExpAsync(command, result, context);
        }

        QsTypeArgumentList ApplyTypeArgumentList(QsTypeArgumentList typeArgList, ImmutableDictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            QsTypeArgumentList? appliedOuter = null;

            if (typeArgList.Outer != null)
                appliedOuter = ApplyTypeArgumentList(typeArgList.Outer, typeEnv);

            var appliedArgs = typeArgList.Args.Select(arg => ApplyTypeValue(arg, typeEnv));

            return QsTypeArgumentList.Make(appliedOuter, appliedArgs);
        }

        QsTypeValue ApplyTypeValue(QsTypeValue typeValue, ImmutableDictionary<QsTypeValue.TypeVar, QsTypeValue> typeEnv)
        {
            switch(typeValue)
            {
                case QsTypeValue.TypeVar typeVar: 
                    return typeEnv[typeVar];

                case QsTypeValue.Normal ntv:
                    {
                        var appliedTypeArgList = ApplyTypeArgumentList(ntv.TypeArgList, typeEnv);
                        return QsTypeValue.MakeNormal(ntv.TypeId, appliedTypeArgList);
                    }

                case QsTypeValue.Void vtv: 
                    return typeValue;

                case QsTypeValue.Func ftv:
                    {
                        var appliedReturn = ApplyTypeValue(ftv.Return, typeEnv);
                        var appliedParams = ImmutableArray.CreateRange(ftv.Params, param => ApplyTypeValue(param, typeEnv));

                        return QsTypeValue.MakeFunc(appliedReturn, appliedParams);
                    }

                default:
                    throw new NotImplementedException();
            }            
        }

        // xType이 y타입인가 묻는 것
        public bool IsType(QsTypeValue xTypeValue, QsTypeValue yTypeValue, QsEvalContext context)
        {
            QsTypeValue? curTypeValue = xTypeValue;

            while (curTypeValue != null)
            {
                if (EqualityComparer<QsTypeValue?>.Default.Equals(curTypeValue, yTypeValue))
                    return true;

                if (!context.TypeValueService.GetBaseTypeValue(curTypeValue, out var baseTypeValue))
                    throw new InvalidOperationException();

                if (baseTypeValue == null)
                    break;

                curTypeValue = baseTypeValue;
            }

            return false;
        }

        // DefaultValue란 무엇인가, 그냥 선언만 되어있는 상태        
        public QsValue GetDefaultValue(QsTypeValue typeValue, QsEvalContext context)
        {
            var typeInst = context.DomainService.GetTypeInst(typeValue);
            return typeInst.MakeDefaultValue();
        }

        public ImmutableArray<QsValue> MakeCaptures(ImmutableArray<QsCaptureInfo.Element> captureElems, QsEvalContext context)
        {
            var captures = ImmutableArray.CreateBuilder<QsValue>(captureElems.Length);
            foreach (var captureElem in captureElems)
            {
                QsValue origValue;
                if (captureElem.StorageInfo is QsStorageInfo.Local localVar)
                    origValue = context.GetLocalVar(localVar.Index);
                else
                    throw new NotImplementedException();

                QsValue value;
                if (captureElem.CaptureKind == QsCaptureKind.Copy)
                {
                    value = origValue!.MakeCopy();
                }
                else
                {
                    Debug.Assert(captureElem.CaptureKind == QsCaptureKind.Ref);
                    value = origValue!;
                }

                captures.Add(value);
            }

            return captures.ToImmutable();
        }

        async IAsyncEnumerable<QsValue> EvaluateScriptFuncInstSeqAsync(
            QsScriptFuncInst scriptFuncInst,
            IReadOnlyList<QsValue> args,
            QsEvalContext context)
        {
            // NOTICE: args가 미리 할당되서 나온 상태
            for (int i = 0; i < args.Count; i++)
                context.InitLocalVar(i, args[i]);

            await foreach (var value in EvaluateStmtAsync(scriptFuncInst.Body, context))
            {
                yield return value;
            }
        }

        public async ValueTask EvaluateVarDeclAsync(VarDecl varDecl, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsVarDeclInfo>(varDecl);

            Debug.Assert(info.Elems.Length == varDecl.Elems.Length);
            for(int i = 0; i < varDecl.Elems.Length; i++)
            {
                var varDeclElem = varDecl.Elems[i];
                var varDeclInfoElem = info.Elems[i];

                var value = GetDefaultValue(varDeclInfoElem.TypeValue, context);

                switch (varDeclInfoElem.StorageInfo)
                {
                    case QsStorageInfo.ModuleGlobal storage:
                        context.DomainService.InitGlobalValue(storage.VarId, value);
                        break;

                    case QsStorageInfo.PrivateGlobal storage:
                        context.InitPrivateGlobalVar(storage.Index, value);
                        break;

                    case QsStorageInfo.Local storage:
                        context.InitLocalVar(storage.Index, value);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                // InitExp가 있으면 
                if (varDeclElem.InitExp != null)
                {
                    await expValueEvaluator.EvalAsync(varDeclElem.InitExp, value, context);
                }          
            }
        }

        public ValueTask EvaluateFuncInstAsync(QsValue? thisValue, QsFuncInst funcInst, IReadOnlyList<QsValue> args, QsValue result, QsEvalContext context)
        {            
            if (funcInst is QsScriptFuncInst scriptFuncInst)
            {
                async ValueTask InnerBodyAsync()
                {
                    await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context)) { }
                }

                // (Capture한 곳의 this), (MemberExp의 this), Static의 경우 this
                if (scriptFuncInst.CapturedThis != null)
                    thisValue = scriptFuncInst.CapturedThis;
                else if (!scriptFuncInst.bThisCall)
                    thisValue = null;

                var localVars = new QsValue?[scriptFuncInst.LocalVarCount];
                for (int i = 0; i < scriptFuncInst.Captures.Length; i++)
                    localVars[i] = scriptFuncInst.Captures[i];

                int argEndIndex = scriptFuncInst.Captures.Length + args.Count;
                for (int i = scriptFuncInst.Captures.Length; i < argEndIndex; i++)
                    localVars[i] = args[i];
                
                // Seq Call 이라면
                if (scriptFuncInst.SeqElemTypeValue != null)
                {
                    // yield에 사용할 공간
                    var yieldValue = GetDefaultValue(scriptFuncInst.SeqElemTypeValue, context);

                    // context 복제
                    var newContext = new QsEvalContext(
                        context,
                        localVars,
                        QsEvalFlowControl.None,
                        ImmutableArray<Task>.Empty,
                        thisValue,
                        yieldValue);

                    var asyncEnum = EvaluateScriptFuncInstSeqAsync(scriptFuncInst, args, newContext);
                    context.RuntimeModule.SetEnumerable(context.DomainService, result, scriptFuncInst.SeqElemTypeValue, asyncEnum);

                    return new ValueTask(Task.CompletedTask);
                }

                return context.ExecInNewFuncFrameAsync(localVars, QsEvalFlowControl.None, ImmutableArray<Task>.Empty, thisValue, result, InnerBodyAsync);
            }
            else if (funcInst is QsNativeFuncInst nativeFuncInst)
            {
                return nativeFuncInst.CallAsync(thisValue, args, result);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public ValueTask EvalExpAsync(Exp exp, QsValue result, QsEvalContext context)
        {
            return expValueEvaluator.EvalAsync(exp, result, context);
        }

        public IAsyncEnumerable<QsValue> EvaluateStmtAsync(Stmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        async ValueTask<int> EvaluateScriptAsync(Script script, QsEvalContext context)
        {
            async ValueTask InnerBodyAsync()
            {
                foreach (var elem in script.Elements)
                {
                    if (elem is StmtScriptElement statementElem)
                    {
                        await foreach (var value in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                        {
                        }
                    }

                    if (context.GetFlowControl() == QsEvalFlowControl.Return)
                        break;
                }
            }

            var info = context.GetNodeInfo<QsScriptInfo>(script);
            var retValue = context.RuntimeModule.MakeInt(0);

            await context.ExecInNewFuncFrameAsync(
                new QsValue?[info.LocalVarCount], 
                QsEvalFlowControl.None, 
                ImmutableArray<Task>.Empty, 
                null, 
                retValue, 
                InnerBodyAsync);

            return context.RuntimeModule.GetInt(retValue);
        }

        public async ValueTask<int?> EvaluateScriptAsync(
            string moduleName,
            Script script,             
            IQsRuntimeModule runtimeModule,
            IEnumerable<IQsMetadata> metadatas,
            IQsErrorCollector errorCollector)
        {
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            var optionalAnalyzeResult = analyzer.AnalyzeScript(moduleName, script, metadatas, errorCollector);
            if (optionalAnalyzeResult == null)
                return null;

            var analyzeResult = optionalAnalyzeResult.Value;

            var scriptModule = new QsScriptModule(
                analyzeResult.ScriptMetadata,
                scriptModule => new QsTypeValueApplier(new QsMetadataService(metadatas.Append(scriptModule))),
                analyzeResult.Templates);

            var domainService = new QsDomainService();

            domainService.LoadModule(runtimeModule);
            domainService.LoadModule(scriptModule);

            var context = new QsEvalContext(
                runtimeModule, 
                domainService, 
                analyzeResult.TypeValueService,                 
                analyzeResult.PrivateGlobalVarCount,
                analyzeResult.InfosByNode);

            return await EvaluateScriptAsync(script, context);
        }
        
        internal QsValue GetMemberValue(QsValue value, QsName varName)
        {
            if (value is QsObjectValue objValue)
                return objValue.GetMemberValue(varName);
            else
                throw new InvalidOperationException();
        }
    }
}