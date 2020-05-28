using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;

namespace QuickSC
{
    class QsStmtEvaluator
    {
        private QsEvaluator evaluator;
        private IQsCommandProvider commandProvider;
        private IQsRuntimeModule runtimeModule;

        public QsStmtEvaluator(QsEvaluator evaluator, IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {
            this.evaluator = evaluator;
            this.commandProvider = commandProvider;
            this.runtimeModule = runtimeModule;
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        internal async ValueTask EvaluateCommandStmtAsync(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdValue = await evaluator.EvaluateStringExpAsync(command, context);
                var cmdText = runtimeModule.GetString(cmdValue);

                await commandProvider.ExecuteAsync(cmdText);
            }
        }

        internal ValueTask EvaluateVarDeclStmtAsync(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return evaluator.EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateIfStmtAsync(QsIfStmt stmt, QsEvalContext context)
        {
            var condValue = await evaluator.EvaluateExpAsync(stmt.Cond, context);

            bool bTestPassed;
            if (stmt.TestType == null)
            {
                bTestPassed = runtimeModule.GetBool(condValue);
            }
            else
            {
                // 분석기가 미리 계산해 놓은 TypeValue를 가져온다
                var ifStmtInfo = (QsIfStmtInfo)context.AnalyzeInfo.InfosByNode[stmt];
                var testTypeInst = evaluator.GetTypeInst(ifStmtInfo.TestTypeValue, context);

                bTestPassed = condValue.IsType(testTypeInst); // typeValue.GetTypeId는 Type의 TypeId일것이다
            }

            if (bTestPassed)
            {
                await foreach (var value in EvaluateStmtAsync(stmt.Body, context))
                    yield return value;
            }
            else
            {
                if (stmt.ElseBody != null)
                    await foreach (var value in EvaluateStmtAsync(stmt.ElseBody, context))
                        yield return value;
            }
        }

        internal async IAsyncEnumerable<QsValue> EvaluateForStmtAsync(QsForStmt forStmt, QsEvalContext context)
        {
            if (forStmt.Initializer != null)
            {
                switch (forStmt.Initializer)
                {
                    case QsExpForStmtInitializer expInitializer:
                        await evaluator.EvaluateExpAsync(expInitializer.Exp, context);
                        break;

                    case QsVarDeclForStmtInitializer varDeclInitializer:
                        await evaluator.EvaluateVarDeclAsync(varDeclInitializer.VarDecl, context);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            while (true)
            {
                if (forStmt.CondExp != null)
                {
                    var condValue = await evaluator.EvaluateExpAsync(forStmt.CondExp, context);                    

                    if (!runtimeModule.GetBool(condValue)) 
                        break;
                }

                await foreach (var value in EvaluateStmtAsync(forStmt.Body, context))
                    yield return value;

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                }
                else if (context.FlowControl is QsReturnEvalFlowControl)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }

                if (forStmt.ContinueExp != null)
                    await evaluator.EvaluateExpAsync(forStmt.ContinueExp, context);
            }
        }

        internal void EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            context.FlowControl = QsContinueEvalFlowControl.Instance;
        }

        internal void EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            context.FlowControl = QsBreakEvalFlowControl.Instance;
        }

        internal async ValueTask EvaluateReturnStmtAsync(QsReturnStmt returnStmt, QsEvalContext context)
        {
            QsValue returnValue;

            if (returnStmt.Value != null)
                returnValue = await evaluator.EvaluateExpAsync(returnStmt.Value, context);
            else
                returnValue = QsVoidValue.Instance;

            context.FlowControl = new QsReturnEvalFlowControl(returnValue);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateBlockStmtAsync(QsBlockStmt blockStmt, QsEvalContext context)
        {
            foreach (var stmt in blockStmt.Stmts)
            {
                await foreach (var value in EvaluateStmtAsync(stmt, context))
                {
                    yield return value;

                    // 확실하지 않아서 걸어둔다
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }

                if (context.FlowControl != QsNoneEvalFlowControl.Instance)
                    break;
            }            
        }

        internal async ValueTask EvaluateExpStmtAsync(QsExpStmt expStmt, QsEvalContext context)
        {
            await evaluator.EvaluateExpAsync(expStmt.Exp, context);
        }

        internal void EvaluateTaskStmt(QsTaskStmt taskStmt, QsEvalContext context)
        {
            var info = (QsTaskStmtInfo)context.AnalyzeInfo.InfosByNode[taskStmt];

            // 1. funcInst로 캡쳐
            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);
            
            var funcInst = new QsScriptFuncInst(
                false,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                taskStmt.Body);

            var newContext = new QsEvalContext(context, new QsValue?[0], QsNoneEvalFlowControl.Instance, ImmutableArray<Task>.Empty, null);

            // 2. 그 funcInst를 바로 실행하기
            var task = Task.Run(async () =>
            {
                await evaluator.EvaluateFuncInstAsync(null, funcInst, ImmutableArray<QsValue>.Empty, newContext);
            });

            context.AddTask(task);
        }

        async IAsyncEnumerable<QsValue> EvaluateAwaitStmtAsync(QsAwaitStmt stmt, QsEvalContext context)
        {
            var prevTasks = context.Tasks;
            context.SetTasks(ImmutableArray<Task>.Empty);            

            await foreach (var value in EvaluateStmtAsync(stmt.Body, context))
                yield return value;
            
            await Task.WhenAll(context.Tasks.ToArray());

            context.SetTasks(prevTasks);
        }

        internal void EvaluateAsyncStmt(QsAsyncStmt asyncStmt, QsEvalContext context)
        {
            var info = (QsAsyncStmtInfo)context.AnalyzeInfo.InfosByNode[asyncStmt];

            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);

            var funcInst = new QsScriptFuncInst(
                false,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                asyncStmt.Body);

            var newContext = new QsEvalContext(context, new QsValue?[0], QsNoneEvalFlowControl.Instance, ImmutableArray<Task>.Empty, null);            

            Func<Task> asyncFunc = async () =>
            {
                await evaluator.EvaluateFuncInstAsync(null, funcInst, ImmutableArray<QsValue>.Empty, newContext);
            };

            var task = asyncFunc();
            context.AddTask(task);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateForeachStmtAsync(QsForeachStmt foreachStmt, QsEvalContext context)
        {
            var info = (QsForeachStmtInfo)context.AnalyzeInfo.InfosByNode[foreachStmt];

            var objValue = await evaluator.EvaluateExpAsync(foreachStmt.Obj, context);
            var getEnumeratorInst = evaluator.GetFuncInst(info.GetEnumeratorValue, context);

            var enumerator = await evaluator.EvaluateFuncInstAsync(objValue, getEnumeratorInst, ImmutableArray<QsValue>.Empty, context);
            var moveNextInst = evaluator.GetFuncInst(info.MoveNextValue, context);
            var getCurrentInst = evaluator.GetFuncInst(info.GetCurrentValue, context);

            var elemTypeInst = evaluator.GetTypeInst(info.ElemTypeValue, context);
            context.LocalVars[info.ElemLocalIndex] = elemTypeInst.MakeDefaultValue();

            while (true)
            {
                var moveNextResult = await evaluator.EvaluateFuncInstAsync(enumerator, moveNextInst, ImmutableArray<QsValue>.Empty, context);
                if (!runtimeModule.GetBool(moveNextResult)) break;

                // GetCurrent
                var getCurrentResult = await evaluator.EvaluateFuncInstAsync(enumerator, getCurrentInst, ImmutableArray<QsValue>.Empty, context);

                // NOTICE: COPY
                context.LocalVars[info.ElemLocalIndex]!.SetValue(getCurrentResult);

                await foreach (var value in EvaluateStmtAsync(foreachStmt.Body, context))
                {
                    yield return value;
                }

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                }
                else if (context.FlowControl is QsReturnEvalFlowControl)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                }
            }
        }

        async IAsyncEnumerable<QsValue> EvaluateYieldStmtAsync(QsYieldStmt yieldStmt, QsEvalContext context)
        {
            yield return await evaluator.EvaluateExpAsync(yieldStmt.Value, context);
        }
        
        internal async IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: await EvaluateCommandStmtAsync(cmdStmt, context); break;
                case QsVarDeclStmt varDeclStmt: await EvaluateVarDeclStmtAsync(varDeclStmt, context); break;
                case QsIfStmt ifStmt:
                    await foreach (var value in EvaluateIfStmtAsync(ifStmt, context))
                        yield return value;
                    break;

                case QsForStmt forStmt:
                    await foreach (var value in EvaluateForStmtAsync(forStmt, context))
                        yield return value;
                    break;

                case QsContinueStmt continueStmt: EvaluateContinueStmt(continueStmt, context); break;
                case QsBreakStmt breakStmt: EvaluateBreakStmt(breakStmt, context); break;
                case QsReturnStmt returnStmt: await EvaluateReturnStmtAsync(returnStmt, context); break;
                case QsBlockStmt blockStmt:
                    await foreach (var result in EvaluateBlockStmtAsync(blockStmt, context))
                        yield return result;
                    break;

                case QsExpStmt expStmt: await EvaluateExpStmtAsync(expStmt, context); break;
                case QsTaskStmt taskStmt: EvaluateTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt:
                    await foreach (var value in EvaluateAwaitStmtAsync(awaitStmt, context))
                        yield return value;
                    break;

                case QsAsyncStmt asyncStmt: EvaluateAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt:
                    await foreach (var value in EvaluateForeachStmtAsync(foreachStmt, context))
                        yield return value;
                    break;

                case QsYieldStmt yieldStmt:
                    await foreach (var result in EvaluateYieldStmtAsync(yieldStmt, context))
                        yield return result;
                    break;

                default: throw new NotImplementedException();
            };
        }
    }
}