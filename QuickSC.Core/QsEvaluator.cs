﻿using System;
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
        private QsExpValueEvaluator expValueEvaluator;
        private QsExpValueLocationEvaluator expValueLocEvaluator;
        private QsStmtEvaluator stmtEvaluator;        

        public QsEvaluator(IQsCommandProvider commandProvider)
        {
            this.expValueEvaluator = new QsExpValueEvaluator(this);
            this.expValueLocEvaluator = new QsExpValueLocationEvaluator();
            this.stmtEvaluator = new QsStmtEvaluator(this, commandProvider);
        }        
        
        public ValueTask EvaluateStringExpAsync(QsStringExp command, QsValue result, QsEvalContext context)
        {
            return expValueEvaluator.EvalStringExpAsync(command, result, context);
        }

        QsTypeValue ApplyTypeValue(QsTypeValue typeValue, ImmutableDictionary<QsTypeValue_TypeVar, QsTypeValue> typeEnv)
        {
            switch(typeValue)
            {
                case QsTypeValue_TypeVar typeVar: 
                    return typeEnv[typeVar];

                case QsTypeValue_Normal ntv:
                    {
                        var appliedOuter = (ntv.Outer != null) ? ApplyTypeValue(ntv.Outer, typeEnv) : null;
                        var appliedTypeArgs = ImmutableArray.CreateRange(ntv.TypeArgs, typeArg => ApplyTypeValue(typeArg, typeEnv));

                        return new QsTypeValue_Normal(appliedOuter, ntv.TypeId, appliedTypeArgs);
                    }

                case QsTypeValue_Void vtv: 
                    return typeValue;

                case QsTypeValue_Func ftv:
                    {
                        var appliedReturn = ApplyTypeValue(ftv.Return, typeEnv);
                        var appliedParams = ImmutableArray.CreateRange(ftv.Params, param => ApplyTypeValue(param, typeEnv));

                        return new QsTypeValue_Func(appliedReturn, appliedParams);
                    }

                default:
                    throw new NotImplementedException();
            }            
        }

        public bool IsType(QsTypeValue subTypeValue, QsTypeValue typeValue, QsEvalContext context)
        {
            QsTypeValue? curTypeValue = subTypeValue;

            while (curTypeValue != null)
            {
                if (EqualityComparer<QsTypeValue?>.Default.Equals(curTypeValue, typeValue))
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
                if (captureElem.Storage is QsStorage.Local localVar)
                    origValue = context.LocalVars[localVar.Index]!;
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

                var value = GetDefaultValue(varDeclInfoElem.TypeValue, context);

                switch (varDeclInfoElem.Storage)
                {
                    case QsStorage.ModuleGlobal storage:
                        context.DomainService.SetGlobalValue(storage.VarId, value);
                        break;

                    case QsStorage.PrivateGlobal storage:
                        context.PrivateGlobalVars[storage.Index] = value;
                        break;

                    case QsStorage.Local storage:
                        // For문에서 재사용할 수 있다
                        // Debug.Assert(context.LocalVars[storage.LocalIndex] == null);
                        context.LocalVars[storage.Index] = value;
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

        public async ValueTask EvaluateFuncInstAsync(QsValue? thisValue, QsFuncInst funcInst, ImmutableArray<QsValue> args, QsValue result, QsEvalContext context)
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

                    return;
                }

                var (prevLocalVars, prevFlowControl, prevTasks, prevThisValue, prevRetValue) = 
                    context.Update(localVars, QsEvalFlowControl.None, ImmutableArray<Task>.Empty, thisValue, result);

                await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context)) { }                

                context.Update(prevLocalVars, prevFlowControl, prevTasks, prevThisValue, prevRetValue);
            }
            else if (funcInst is QsNativeFuncInst nativeFuncInst)
            {
                await nativeFuncInst.CallAsync(thisValue, args, result);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public QsValue EvalValueLocExp(QsExp exp, QsEvalContext context)
        {
            return expValueLocEvaluator.Eval(exp, context);
        }

        public ValueTask EvaluateExpAsync(QsExp exp, QsValue result, QsEvalContext context)
        {
            return expValueEvaluator.EvalAsync(exp, result, context);
        }

        public IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        async ValueTask<int> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            var info = (QsScriptInfo)context.AnalyzeInfo.InfosByNode[script];

            var retValue = context.RuntimeModule.MakeInt(0);

            context.Update(new QsValue?[info.LocalVarCount], QsEvalFlowControl.None, ImmutableArray<Task>.Empty, null, retValue);

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var value in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                    }
                }

                if (context.FlowControl == QsEvalFlowControl.Return)
                    break;
            }

            return context.RuntimeModule.GetInt(retValue);
        }

        public async ValueTask<int> EvaluateScriptAsync(
            QsScript script, 
            IQsRuntimeModule runtimeModule,
            QsDomainService domainService,
            QsTypeValueService typeValueService,
            QsStaticValueService staticValueService, 
            QsAnalyzeInfo analyzeInfo)
        {
            var context = new QsEvalContext(runtimeModule, domainService, typeValueService, staticValueService, analyzeInfo);
            return await EvaluateScriptAsync(script, context);
        }

        public QsValue GetStaticValue(QsVarValue varValue, QsEvalContext context)
        {
            return context.StaticValueService.GetValue(varValue);
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