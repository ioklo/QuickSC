using QuickSC.Runtime;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    class QsCallableEvaluator
    {
        private QsStmtEvaluator stmtEvaluator;
        private IQsRuntimeModule runtimeModule;

        public QsCallableEvaluator(QsStmtEvaluator stmtEvaluator, IQsRuntimeModule runtimeModule)
        {
            this.stmtEvaluator = stmtEvaluator;
            this.runtimeModule = runtimeModule;
        }

        async IAsyncEnumerable<QsEvalResult<QsValue>> EvaluateSequenceCallAsync(QsStmt body, QsValue thisValue, ImmutableDictionary<string, QsValue> vars, QsEvalContext context)
        {
            // 프레임 전환 
            var (prevThisValue, prevVars, prevTasks) = (context.ThisValue, context.LocalVars, context.Tasks);

            context = context.SetThisValue(thisValue).SetLocalVars(vars).SetTasks(ImmutableArray<Task>.Empty);

            // 현재 funcContext
            await foreach (var result in stmtEvaluator.EvaluateStmtAsync(body, context))
            {
                if (!result.HasValue) { yield return QsEvalResult<QsValue>.Invalid; yield break; }

                context = result.Value;

                if (context.FlowControl is QsYieldEvalFlowControl yieldFlowControl)
                {
                    yield return new QsEvalResult<QsValue>(yieldFlowControl.Value, context);
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
            }

            // context = context.SetVars(prevVars).SetTasks(prevTasks).SetThisValue(prevThisValue);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateNormalCallAsync(QsStmt body, QsValue thisValue, ImmutableDictionary<string, QsValue> vars, QsEvalContext context)
        {
            // 프레임 전환 
            var (prevThisValue, prevVars, prevTasks) = (context.ThisValue, context.LocalVars, context.Tasks);

            context = context.SetThisValue(thisValue).SetLocalVars(vars).SetTasks(ImmutableArray<Task>.Empty);

            // 현재 funcContext
            await foreach (var result in stmtEvaluator.EvaluateStmtAsync(body, context))
            {
                if (!result.HasValue) return QsEvalResult<QsValue>.Invalid;

                context = result.Value;

                if (context.FlowControl is QsYieldEvalFlowControl yieldFlowControl)
                    throw new InvalidOperationException();
            }

            context = context.SetLocalVars(prevVars).SetTasks(prevTasks).SetThisValue(prevThisValue);

            if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(returnFlowControl.Value, context);
            }
            else
            {
                context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                return new QsEvalResult<QsValue>(QsNullValue.Instance, context);
            }
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateFuncCallableAsync(QsFuncCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (callable.FuncDecl.Params.Length != args.Length)
                return QsEvalResult<QsValue>.Invalid;

            // captures 
            var vars = ImmutableDictionary.CreateBuilder<string, QsValue>();

            for (int i = 0; i < args.Length; i++)
                vars.Add(callable.FuncDecl.Params[i].Name, args[i]);

            if (callable.FuncDecl.FuncKind == QsFuncKind.Sequence)
            {
                // context.. 여기 들어가 있어도 괜찮은걸까
                var asyncEnum = EvaluateSequenceCallAsync(callable.FuncDecl.Body, thisValue, vars.ToImmutable(), context);
                return new QsEvalResult<QsValue>(new QsObjectValue(runtimeModule.MakeAsyncEnumerableObject(ToValue(asyncEnum))), context);
            }
            else
            {
                Debug.Assert(callable.FuncDecl.FuncKind == QsFuncKind.Normal);

                return await EvaluateNormalCallAsync(callable.FuncDecl.Body, thisValue, vars.ToImmutable(), context);
            }

            static async IAsyncEnumerable<QsValue> ToValue(IAsyncEnumerable<QsEvalResult<QsValue>> e)
            {
                await foreach (var result in e)
                    yield return result.Value;
            }
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateLambdaCallableAsync(QsLambdaCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (callable.Exp.Params.Length != args.Length)
                return QsEvalResult<QsValue>.Invalid;

            // captures 
            var vars = callable.Captures.ToBuilder();

            for (int i = 0; i < args.Length; i++)
                vars.Add(callable.Exp.Params[i].Name, args[i]);

            // TODO: 표현이 좀 이상하므로 다시 생각해볼 것
            return await EvaluateNormalCallAsync(callable.Exp.Body, thisValue, vars.ToImmutable(), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateNativeCallableAsync(QsNativeCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            var result = await callable.Invoker(thisValue, args);
            if (result == null)
                return QsEvalResult<QsValue>.Invalid;

            return new QsEvalResult<QsValue>(result, context);
        }

        internal async ValueTask<QsEvalResult<QsValue>> EvaluateCallableAsync(QsCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            return callable switch
            {
                QsFuncCallable funcCallable => await EvaluateFuncCallableAsync(funcCallable, thisValue, args, context),
                QsLambdaCallable lambdaCallable => await EvaluateLambdaCallableAsync(lambdaCallable, thisValue, args, context),
                QsNativeCallable nativeCallable => await EvaluateNativeCallableAsync(nativeCallable, thisValue, args, context),
                _ => throw new NotImplementedException()
            };
        }

    }
}
