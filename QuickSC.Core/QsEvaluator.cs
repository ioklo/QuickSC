using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Runtime;
using QuickSC.Syntax;

namespace QuickSC
{
    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        QsExpEvaluator expEvaluator;
        QsStmtEvaluator stmtEvaluator;

        public QsEvaluator(IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {            
            this.expEvaluator = new QsExpEvaluator(this, runtimeModule);
            this.stmtEvaluator = new QsStmtEvaluator(this, expEvaluator, commandProvider, runtimeModule);
        }

        // virtual이냐 아니냐만 해도 될것 같다
        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public QsFuncInst GetFuncInst(QsValue thisValue, QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsTypeExp typeExp)
        {
            // context.TypeValuesByTypeExp[typeExp]

            throw new NotImplementedException();
        }

        async IAsyncEnumerable<QsValue> EvaluateScriptFuncInstSeqAsync(
            QsScriptFuncInst scriptFuncInst,
            ImmutableArray<QsValue> args,
            QsEvalContext context)
        {
            Debug.Assert(scriptFuncInst.Params.Length == args.Length);
            for (int i = 0; i < args.Length; i++)
                context.SetLocalVar(scriptFuncInst.Params[i], args[i]);

            await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context))
            {
                if (context.FlowControl is QsYieldEvalFlowControl yieldFlowControl)
                    yield return yieldFlowControl.Value;
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
                    QsEvalContext newContext = new QsEvalContext(
                        context.TypeValuesByExp,
                        context.TypeValuesByTypeExp,
                        context.StoragesByExp,
                        context.CaptureInfosByLocation,
                        scriptFuncInst.Captures,
                        QsNoneEvalFlowControl.Instance,
                        ImmutableArray<Task>.Empty,
                        thisValue,
                        false);

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

                await foreach (var _ in evaluator.EvaluateStmtAsync(scriptFuncInst.Body, context)) { }

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



        public IAsyncEnumerable<QsEvalContext?> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        public async ValueTask<QsEvalContext?> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var result in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                        if (!result.HasValue) return null;
                        context = result.Value;
                    }
                }
            }

            return context;
        }

        internal QsTypeInst InstantiateType(QsTypeValue testTypeValue, QsEvalContext context)
        {
            return new QsRawTypeInst(testTypeValue);
        }
    }
}