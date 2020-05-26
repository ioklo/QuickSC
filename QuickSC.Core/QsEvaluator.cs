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
        QsDomainService domainService;

        QsExpEvaluator expEvaluator;
        QsStmtEvaluator stmtEvaluator;
        IQsRuntimeModule runtimeModule;

        public QsEvaluator(QsDomainService domainService, IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {
            this.domainService = domainService;

            this.expEvaluator = new QsExpEvaluator(this, runtimeModule, domainService);
            this.stmtEvaluator = new QsStmtEvaluator(this, expEvaluator, commandProvider, runtimeModule);
            this.runtimeModule = runtimeModule;
        }
        
        public QsFuncInst GetFuncInst(QsValue thisValue, QsFuncValue funcValue, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsTypeValue typeValue, QsEvalContext context)
        {   
            // context.TypeValuesByTypeExp[typeExp]
            // return new QsRawTypeInst(testTypeValue);

            throw new NotImplementedException();
        }
        
        internal QsValue GetDefaultValue(QsTypeValue typeValue, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<QsValue> MakeCaptures(ImmutableArray<QsLambdaExpInfo.Elem> captureElems, QsEvalContext context)
        {
            var captures = ImmutableArray.CreateBuilder<QsValue>(captureElems.Length);
            foreach (var captureElem in captureElems)
            {
                QsValue origValue;
                if (captureElem.Kind is QsLambdaExpInfo.Elem.ExpKind.GlobalVar globalVar)
                    origValue = context.GlobalVars[globalVar.VarId];
                else if (captureElem.Kind is QsLambdaExpInfo.Elem.ExpKind.LocalVar localVar)
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

        public async ValueTask<QsValue> EvaluateFuncInstAsync(QsValue? thisValue, QsFuncInst funcInst, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (funcInst is QsScriptFuncInst scriptFuncInst)
            {
                // (Capture한 곳의 this), (MemberExp의 this), Static의 경우 this
                if (scriptFuncInst.CapturedThis != null)
                    thisValue = scriptFuncInst.CapturedThis;
                else if (!scriptFuncInst.bThisCall)
                    thisValue = null;

                if (scriptFuncInst.bSeqCall)
                {
                    // context 복제
                    var newContext = new QsEvalContext(
                        context,
                        scriptFuncInst.Captures,
                        ImmutableArray<Task>.Empty,
                        QsNoneEvalFlowControl.Instance,
                        thisValue);

                    var asyncEnum = EvaluateScriptFuncInstSeqAsync(scriptFuncInst, args, newContext);
                    return runtimeModule.MakeAsyncEnumerable(asyncEnum);
                }

                var (prevLocalVars, prevTasks, prevThisValue) = (context.GetLocalVars(), context.GetTasks(), context.ThisValue);

                context.SetLocalVars(scriptFuncInst.Captures)
                        .SetTasks(ImmutableArray<Task>.Empty);
                context.ThisValue = thisValue;

                Debug.Assert(scriptFuncInst.Params.Length == args.Length);
                for (int i = 0; i < args.Length; i++)
                    context.SetLocalVar(scriptFuncInst.Params[i], args[i]);

                await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context)) { }

                context.SetLocalVars(prevLocalVars)
                        .SetTasks(prevTasks);
                context.ThisValue = prevThisValue;

                if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                    return returnFlowControl.Value;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                    return QsVoidValue.Instance;
                }
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


        public IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        async ValueTask EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            // 모듈은 Script를 묶은 것이므로 여기가 아니다
            // 일단 여기서             
            InitGlobalVariables(context);

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var value in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                    }
                }
            }
        }

        public async ValueTask<bool> EvaluateScriptAsync(QsScript script)
        {
            var context = new QsEvalContext();
            await EvaluateScriptAsync(script, context);
        }
    }
}