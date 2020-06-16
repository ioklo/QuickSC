using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;

namespace QuickSC
{
    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        QsExpEvaluator expEvaluator;
        QsStmtEvaluator stmtEvaluator;        

        public QsEvaluator(IQsCommandProvider commandProvider)
        {
            this.expEvaluator = new QsExpEvaluator(this);
            this.stmtEvaluator = new QsStmtEvaluator(this, commandProvider);
        }        
        
        public ValueTask<QsValue> EvaluateStringExpAsync(QsStringExp command, QsEvalContext context)
        {
            return expEvaluator.EvaluateStringExpAsync(command, context);
        }

        QsTypeValue ApplyTypeValue(QsTypeValue typeValue, ImmutableDictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            switch(typeValue)
            {
                case QsTypeVarTypeValue typeVar: 
                    return typeEnv[typeVar];

                case QsNormalTypeValue ntv:
                    {
                        var appliedOuter = (ntv.Outer != null) ? ApplyTypeValue(ntv.Outer, typeEnv) : null;
                        var appliedTypeArgs = ImmutableArray.CreateRange(ntv.TypeArgs, typeArg => ApplyTypeValue(typeArg, typeEnv));

                        return new QsNormalTypeValue(appliedOuter, ntv.TypeId, appliedTypeArgs);
                    }

                case QsVoidTypeValue vtv: 
                    return typeValue;

                case QsFuncTypeValue ftv:
                    {
                        var appliedReturn = ApplyTypeValue(ftv.Return, typeEnv);
                        var appliedParams = ImmutableArray.CreateRange(ftv.Params, param => ApplyTypeValue(param, typeEnv));

                        return new QsFuncTypeValue(appliedReturn, appliedParams);
                    }

                default:
                    throw new NotImplementedException();
            }            
        }

        public bool IsType(QsNormalTypeValue subTypeValue, QsNormalTypeValue typeValue, QsEvalContext context)
        {
            QsNormalTypeValue? curTypeValue = subTypeValue;

            while (curTypeValue != null)
            {
                if (EqualityComparer<QsTypeValue?>.Default.Equals(curTypeValue, typeValue)) return true;

                if (!context.DomainService.GetBaseTypeValue(curTypeValue, out var baseTypeValue))
                    throw new InvalidOperationException();

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
                if (captureElem.Storage is QsLocalStorage localVar)
                    origValue = context.LocalVars[localVar.LocalIndex]!;
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
            ImmutableArray<QsValue> args,
            QsEvalContext context)
        {
            for (int i = 0; i < args.Length; i++)
                context.LocalVars[i] = args[i];

            await foreach (var value in EvaluateStmtAsync(scriptFuncInst.Body, context))
            {
                yield return value;
            }
        }

        public async ValueTask EvaluateVarDeclAsync(QsVarDecl varDecl, QsEvalContext context)
        {
            var info = (QsVarDeclInfo)context.AnalyzeInfo.InfosByNode[varDecl];

            Debug.Assert(info.Elems.Length == varDecl.Elems.Length);
            for(int i = 0; i < varDecl.Elems.Length; i++)
            {
                var varDeclElem = varDecl.Elems[i];
                var varDeclInfoElem = info.Elems[i];

                QsValue value;

                // InitExp가 있으면 그 Value의 MakeCopy,
                if (varDeclElem.InitExp != null)
                {
                    QsValue origValue = await expEvaluator.EvaluateExpAsync(varDeclElem.InitExp, context);
                    value = origValue.MakeCopy();
                }
                else
                {
                    // 없으면 defaultValue
                    value = GetDefaultValue(varDeclInfoElem.TypeValue, context);
                }

                switch(varDeclInfoElem.Storage)
                {
                    case QsGlobalStorage storage:
                        Debug.Assert(!context.GlobalVars.ContainsKey(storage.VarId));
                        context.GlobalVars[storage.VarId] = value.MakeCopy();
                        break;

                    case QsLocalStorage storage: 
                        // For문에서 재사용할 수 있다
                        // Debug.Assert(context.LocalVars[storage.LocalIndex] == null);
                        context.LocalVars[storage.LocalIndex] = value;
                        break;

                    default:
                        throw new NotImplementedException();
                }                
            }
        }

        public async ValueTask<QsValue> EvaluateFuncInstAsync(QsValue? thisValue, QsFuncInst funcInst, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (funcInst is QsScriptFuncInst scriptFuncInst)
            {
                // (Capture한 곳의 this), (MemberExp의 this), Static의 경우 this
                if (scriptFuncInst.CapturedThis != null)
                    thisValue = scriptFuncInst.CapturedThis;
                else if (!scriptFuncInst.bThisCall)
                    thisValue = null;

                var localVars = new QsValue?[scriptFuncInst.LocalVarCount];
                for (int i = 0; i < scriptFuncInst.Captures.Length; i++)
                    localVars[i] = scriptFuncInst.Captures[i];

                int argEndIndex = scriptFuncInst.Captures.Length + args.Length;
                for (int i = scriptFuncInst.Captures.Length; i < argEndIndex; i++)
                    localVars[i] = args[i];
                
                // Seq Call 이라면
                if (scriptFuncInst.SeqElemTypeValue != null)
                {
                    // context 복제
                    var newContext = new QsEvalContext(
                        context,
                        localVars,
                        QsNoneEvalFlowControl.Instance,
                        ImmutableArray<Task>.Empty,
                        thisValue);

                    var asyncEnum = EvaluateScriptFuncInstSeqAsync(scriptFuncInst, args, newContext);
                    return context.RuntimeModule.MakeEnumerable(context.DomainService, (QsNormalTypeValue)scriptFuncInst.SeqElemTypeValue, asyncEnum);
                }

                var (prevLocalVars, prevFlowControl, prevTasks, prevThisValue) = 
                    context.Update(localVars, QsNoneEvalFlowControl.Instance, ImmutableArray<Task>.Empty, thisValue);

                await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context)) { }

                QsValue retValue;
                if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
                {   
                    retValue = returnFlowControl.Value;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                    retValue = QsVoidValue.Instance;
                }

                context.Update(prevLocalVars, prevFlowControl, prevTasks, prevThisValue);
                return retValue;
            }
            else if (funcInst is QsNativeFuncInst nativeFuncInst)
            {
                return await nativeFuncInst.CallAsync(thisValue, args);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public ValueTask<QsValue> EvaluateExpAsync(QsExp exp, QsEvalContext context)
        {
            return expEvaluator.EvaluateExpAsync(exp, context);
        }

        public IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        async ValueTask<int> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            var info = (QsScriptInfo)context.AnalyzeInfo.InfosByNode[script];

            context.Update(new QsValue?[info.LocalVarCount], QsNoneEvalFlowControl.Instance, ImmutableArray<Task>.Empty, null);

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var value in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                    }
                }

                if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
                {
                    return context.RuntimeModule.GetInt(returnFlowControl.Value);
                }
            }

            return 0;
        }

        public async ValueTask<int> EvaluateScriptAsync(
            QsScript script, 
            IQsRuntimeModule runtimeModule,
            QsDomainService domainService, 
            QsStaticValueService staticValueService, 
            QsAnalyzeInfo analyzeInfo)
        {
            var context = new QsEvalContext(runtimeModule, domainService, staticValueService, analyzeInfo);
            return await EvaluateScriptAsync(script, context);
        }

        public QsValue GetStaticValue(QsVarValue varValue, QsEvalContext context)
        {
            return context.StaticValueService.GetValue(varValue);
        }
    }
}