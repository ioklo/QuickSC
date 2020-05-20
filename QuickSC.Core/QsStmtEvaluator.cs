﻿using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private QsExpEvaluator expEvaluator;
        private IQsCommandProvider commandProvider;
        private IQsRuntimeModule runtimeModule;

        public QsStmtEvaluator(QsEvaluator evaluator, QsExpEvaluator expEvaluator, IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {
            this.evaluator = evaluator;
            this.expEvaluator = expEvaluator;
            this.commandProvider = commandProvider;
            this.runtimeModule = runtimeModule;
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        internal async ValueTask EvaluateCommandStmtAsync(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdValue = await expEvaluator.EvaluateStringExpAsync(command, context);                
                var cmdText = runtimeModule.GetString(cmdValue);                

                await commandProvider.ExecuteAsync(cmdText);
            }
        }

        internal ValueTask EvaluateVarDeclStmtAsync(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async ValueTask EvaluateVarDeclAsync(QsVarDecl varDecl, QsEvalContext context)
        {
            foreach (var elem in varDecl.Elements)
            {
                QsValue? value = null;

                if (elem.InitExp != null)
                    value = await expEvaluator.EvaluateExpAsync(elem.InitExp, context);

                if (context.bGlobalScope)
                    context.GlobalVars.Add((null, elem.VarName), value);
                else
                    context.SetLocalVar(elem.VarName, value);
            }
        }

        internal async IAsyncEnumerable<QsValue> EvaluateIfStmtAsync(QsIfStmt stmt, QsEvalContext context)
        {
            var bPrevGlobalScope = context.bGlobalScope;
            context.bGlobalScope = false;

            var condValue = await expEvaluator.EvaluateExpAsync(stmt.Cond, context);            

            bool bTestPassed;
            if (stmt.TestType == null)
            {
                bTestPassed = runtimeModule.GetBool(condValue);
            }
            else
            {
                // 타입체커가 미리 계산해 놓은 TypeValue를 가져온다
                var testTypeValue = context.TypeValuesByTypeExp[stmt.TestType];
                var testTypeInst = evaluator.GetTypeInst(testTypeValue, context);

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

            context.bGlobalScope = bPrevGlobalScope;
        }

        internal async IAsyncEnumerable<QsValue> EvaluateForStmtAsync(QsForStmt forStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.LocalVars, context.bGlobalScope);
            context.bGlobalScope = false;

            if (forStmt.Initializer != null)
            {
                switch (forStmt.Initializer)
                {
                    case QsExpForStmtInitializer expInitializer:
                        await expEvaluator.EvaluateExpAsync(expInitializer.Exp, context);
                        break;

                    case QsVarDeclForStmtInitializer varDeclInitializer:
                        await EvaluateVarDeclAsync(varDeclInitializer.VarDecl, context);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            while (true)
            {
                if (forStmt.CondExp != null)
                {
                    var condValue = await expEvaluator.EvaluateExpAsync(forStmt.CondExp, context);                    

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
                    await expEvaluator.EvaluateExpAsync(forStmt.ContinueExp, context);
            }

            context.SetLocalVars(prevVars);
            context.bGlobalScope = bPrevGlobalScope;
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
                returnValue = await expEvaluator.EvaluateExpAsync(returnStmt.Value, context);
            else
                returnValue = QsVoidValue.Instance;

            context.FlowControl = new QsReturnEvalFlowControl(returnValue);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateBlockStmtAsync(QsBlockStmt blockStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.LocalVars, context.bGlobalScope);
            context.bGlobalScope = false;

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

            context.SetLocalVars(prevVars);
            context.bGlobalScope = bPrevGlobalScope;
        }

        internal async ValueTask EvaluateExpStmtAsync(QsExpStmt expStmt, QsEvalContext context)
        {
            await expEvaluator.EvaluateExpAsync(expStmt.Exp, context);
        }

        internal void EvaluateTaskStmt(QsTaskStmt taskStmt, QsEvalContext context)
        {
            var captureInfo = context.CaptureInfosByLocation[QsCaptureInfoLocation.Make(taskStmt)];
            var captures = evaluator.MakeCaptures(captureInfo, context);

            var newContext = context.MakeCopy();
            newContext.SetLocalVars(captures);
            newContext.bGlobalScope = false;

            var task = Task.Run(async () =>
            {
                await foreach (var result in EvaluateStmtAsync(taskStmt.Body, context))
                {
                }
            });

            context.AddTask(task);
        }
        
        async IAsyncEnumerable<QsValue> EvaluateAwaitStmtAsync(QsAwaitStmt stmt, QsEvalContext context)
        {
            var (prevTasks, prevVars, bPrevGlobalScope) = (context.Tasks, context.LocalVars, context.bGlobalScope);
            context.SetTasks(ImmutableArray<Task>.Empty);
            context.bGlobalScope = false;

            await foreach (var value in EvaluateStmtAsync(stmt.Body, context))
                yield return value;

            await Task.WhenAll(context.Tasks.ToArray());

            context.SetTasks(prevTasks)
                .SetLocalVars(prevVars);

            context.bGlobalScope = bPrevGlobalScope;
        }

        internal void EvaluateAsyncStmt(QsAsyncStmt asyncStmt, QsEvalContext context)
        {
            var captureInfo = context.CaptureInfosByLocation[QsCaptureInfoLocation.Make(asyncStmt)];
            var captures = evaluator.MakeCaptures(captureInfo, context);

            var newContext = context.MakeCopy();
            newContext.SetLocalVars(captures);
            newContext.bGlobalScope = false;

            Func<Task> asyncFunc = async () =>
            {
                await foreach (var result in EvaluateStmtAsync(asyncStmt.Body, newContext))
                {
                }
            };

            var task = asyncFunc();
            context.AddTask(task);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateForeachStmtAsync(QsForeachStmt foreachStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.LocalVars, context.bGlobalScope);
            context.bGlobalScope = false;

            var info = context.ForeachInfosByForEachStmt[foreachStmt];

            var objValue = await expEvaluator.EvaluateExpAsync(foreachStmt.Obj, context);
            var getEnumeratorInst = evaluator.GetFuncInst(objValue, info.GetEnumeratorValue, ImmutableArray<QsTypeInst>.Empty, context);

            var enumerator = await evaluator.EvaluateFuncInstAsync(objValue, getEnumeratorInst, ImmutableArray<QsValue>.Empty, context);
            var moveNextInst = evaluator.GetFuncInst(enumerator, info.MoveNextValue, ImmutableArray<QsTypeInst>.Empty, context);
            var getCurrentInst = evaluator.GetFuncInst(enumerator, info.GetCurrentValue, ImmutableArray<QsTypeInst>.Empty, context);

            while (true)
            {
                var moveNextResult = await evaluator.EvaluateFuncInstAsync(enumerator, moveNextInst, ImmutableArray<QsValue>.Empty, context);
                if (!runtimeModule.GetBool(moveNextResult)) break;

                // GetCurrent
                var getCurrentResult = await evaluator.EvaluateFuncInstAsync(enumerator, getCurrentInst, ImmutableArray<QsValue>.Empty, context);

                // NOTICE: COPY
                context.SetLocalVar(foreachStmt.VarName, getCurrentResult.MakeCopy());

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

            context.SetLocalVars(prevVars);
            context.bGlobalScope = bPrevGlobalScope;
        }

        async IAsyncEnumerable<QsValue> EvaluateYieldStmtAsync(QsYieldStmt yieldStmt, QsEvalContext context)
        {
            yield return await expEvaluator.EvaluateExpAsync(yieldStmt.Value, context);
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