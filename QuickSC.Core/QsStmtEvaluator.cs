using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;

namespace QuickSC
{
    class QsStmtEvaluator
    {
        private QsEvaluator evaluator;
        private QsExpEvaluator expEvaluator;
        private IQsCommandProvider commandProvider;

        public QsStmtEvaluator(QsEvaluator evaluator, QsExpEvaluator expEvaluator, IQsCommandProvider commandProvider)
        {
            this.evaluator = evaluator;
            this.expEvaluator = expEvaluator;
            this.commandProvider = commandProvider;
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        internal async ValueTask<QsEvalContext?> EvaluateCommandStmtAsync(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdResult = await expEvaluator.EvaluateStringExpAsync(command, context);
                if (!cmdResult.HasValue) return null;
                context = cmdResult.Context;

                var cmdText = GetString(cmdResult.Value);
                if (cmdText == null) return null;

                await commandProvider.ExecuteAsync(cmdText);
            }
            return context;
        }

        internal async ValueTask<QsEvalContext?> EvaluateVarDeclStmtAsync(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return await EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async ValueTask<QsEvalContext?> EvaluateVarDeclAsync(QsVarDecl varDecl, QsEvalContext context)
        {
            foreach (var elem in varDecl.Elements)
            {
                QsValue value;
                if (elem.InitExp != null)
                {
                    var expResult = await expEvaluator.EvaluateExpAsync(elem.InitExp, context);
                    if (!expResult.HasValue)
                        return null;

                    value = expResult.Value;
                    context = expResult.Context;
                }
                else
                {
                    value = QsNullValue.Instance;
                }

                if (context.bGlobalScope)
                    context = context.SetGlobalValue(elem.VarName, value);
                else
                    context = context.SetValue(elem.VarName, value);
            }

            return context;
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateIfStmtAsync(QsIfStmt stmt, QsEvalContext context)
        {
            var bPrevGlobalScope = context.bGlobalScope;
            context = context.SetGlobalScope(false);

            if (!Eval(await expEvaluator.EvaluateExpAsync(stmt.Cond, context), ref context, out var condValue))
            { 
                yield return null; yield break; 
            }

            bool bTestPassed;
            if (stmt.TestType == null)
            {
                var condBoolValue = condValue! as QsValue<bool>;
                if (condBoolValue == null) { yield return null; yield break; }

                bTestPassed = condBoolValue.Value;
            }
            else
            {
                // 타입체커가 미리 계산해 놓은 TypeValue를 가져온다
                var testTypeValue = context.GetTypeValue(stmt.TestType);
                if (testTypeValue == null) { yield return null; yield break; }

                var testTypeInst = evaluator.InstantiateType(testTypeValue, context);

                bTestPassed = condValue!.IsType(testTypeInst); // typeValue.GetTypeId는 Type의 TypeId일것이다
            }

            if (bTestPassed)
            {
                await foreach (var result in EvaluateStmtAsync(stmt.Body, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }
            }
            else
            {
                if (stmt.ElseBody != null)
                {
                    await foreach (var result in EvaluateStmtAsync(stmt.ElseBody, context))
                    {
                        if (!result.HasValue)
                        {
                            yield return null;
                            yield break;
                        }

                        context = result.Value;
                        if (context.FlowControl is QsYieldEvalFlowControl)
                        {
                            yield return context;
                            context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                        }
                    }
                }
            }
            
            yield return context.SetGlobalScope(bPrevGlobalScope);
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateForStmtAsync(QsForStmt forStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.Vars, context.bGlobalScope);
            context = context.SetGlobalScope(false);

            switch (forStmt.Initializer)
            {
                case QsExpForStmtInitializer expInitializer:
                    {
                        var valueResult = await expEvaluator.EvaluateExpAsync(expInitializer.Exp, context);
                        if (!valueResult.HasValue) { yield return null; yield break; }
                        context = valueResult.Context;
                        break;
                    }
                case QsVarDeclForStmtInitializer varDeclInitializer:
                    {
                        var evalResult = await EvaluateVarDeclAsync(varDeclInitializer.VarDecl, context);
                        if (!evalResult.HasValue) { yield return null; yield break; }
                        context = evalResult.Value;
                        break;
                    }

                case null:
                    break;

                default:
                    throw new NotImplementedException();
            }

            while (true)
            {
                if (forStmt.CondExp != null)
                {
                    var condExpResult = await expEvaluator.EvaluateExpAsync(forStmt.CondExp, context);
                    if (!condExpResult.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    var condExpBoolValue = condExpResult.Value as QsValue<bool>;
                    if (condExpBoolValue == null)
                    {
                        yield return null;
                        yield break;
                    }

                    context = condExpResult.Context;
                    if (!condExpBoolValue.Value)
                        break;
                }

                await foreach (var result in EvaluateStmtAsync(forStmt.Body, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
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
                {
                    var contExpResult = await expEvaluator.EvaluateExpAsync(forStmt.ContinueExp, context);
                    if (!contExpResult.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = contExpResult.Context;
                }
            }

            yield return context.SetVars(prevVars).SetGlobalScope(bPrevGlobalScope);
        }

        internal QsEvalContext? EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsContinueEvalFlowControl.Instance);
        }

        internal QsEvalContext? EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsBreakEvalFlowControl.Instance);
        }

        internal async ValueTask<QsEvalContext?> EvaluateReturnStmtAsync(QsReturnStmt returnStmt, QsEvalContext context)
        {
            QsValue returnValue;
            if (returnStmt.Value != null)
            {
                var returnValueResult = await expEvaluator.EvaluateExpAsync(returnStmt.Value, context);
                if (!returnValueResult.HasValue)
                    return null;

                returnValue = returnValueResult.Value;
            }
            else
            {
                returnValue = QsNullValue.Instance;
            }

            return context.SetFlowControl(new QsReturnEvalFlowControl(returnValue));
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateBlockStmtAsync(QsBlockStmt blockStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.Vars, context.bGlobalScope);
            context = context.SetGlobalScope(false);

            foreach (var stmt in blockStmt.Stmts)
            {
                await foreach (var result in EvaluateStmtAsync(stmt, context))
                {
                    if (!result.HasValue)
                    {
                        yield return null;
                        yield break;
                    }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                    else if (context.FlowControl != QsNoneEvalFlowControl.Instance)
                    {
                        yield return context.SetVars(prevVars);
                        yield break;
                    }
                }
            }

            yield return context.SetVars(prevVars).SetGlobalScope(bPrevGlobalScope);
        }

        internal async ValueTask<QsEvalContext?> EvaluateExpStmtAsync(QsExpStmt expStmt, QsEvalContext context)
        {
            var expResult = await expEvaluator.EvaluateExpAsync(expStmt.Exp, context);
            if (!expResult.HasValue) return null;

            return expResult.Context;
        }

        internal QsEvalContext? EvaluateTaskStmt(QsTaskStmt taskStmt, QsEvalContext context)
        {
            var captureInfo = context.StaticContext.CaptureInfosByLocation[QsCaptureInfoLocation.Make(taskStmt)];

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var (name, kind) in captureInfo)
            {
                var origValue = context.GetValue(name);

                if (origValue == null)
                    origValue = context.GetGlobalValue(name);

                if (origValue == null) return null;

                QsValue value;
                if (kind == QsCaptureContextCaptureKind.Copy)
                {
                    value = origValue.MakeCopy();
                }
                else
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            var newContext = QsEvalContext.Make(context.StaticContext);
            newContext = newContext.SetVars(captures.ToImmutable()).SetGlobalScope(false);

            var task = Task.Run(async () =>
            {
                await foreach (var result in EvaluateStmtAsync(taskStmt.Body, context))
                {
                    if (!result.HasValue) return;
                    context = result.Value;
                }
            });

            return context.AddTask(task);
        }

        async IAsyncEnumerable<QsEvalContext?> EvaluateAwaitStmtAsync(QsAwaitStmt stmt, QsEvalContext context)
        {
            var (prevTasks, prevVars, bPrevGlobalScope) = (context.Tasks, context.Vars, context.bGlobalScope);
            context = context.SetTasks(ImmutableArray<Task>.Empty).SetGlobalScope(false);

            await foreach (var result in EvaluateStmtAsync(stmt.Body, context))
            {
                if (!result.HasValue)
                {
                    yield return null;
                    yield break;
                }

                context = result.Value;
                if (context.FlowControl is QsYieldEvalFlowControl)
                {
                    yield return context;
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                }
            }

            await Task.WhenAll(context.Tasks.ToArray());

            yield return context.SetTasks(prevTasks).SetVars(prevVars).SetGlobalScope(bPrevGlobalScope);
        }

        internal QsEvalContext? EvaluateAsyncStmt(QsAsyncStmt asyncStmt, QsEvalContext context)
        {
            var captureInfo = context.StaticContext.CaptureInfosByLocation[QsCaptureInfoLocation.Make(asyncStmt)];

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var (name, kind) in captureInfo)
            {
                var origValue = context.GetValue(name);

                if (origValue == null)
                    origValue = context.GetGlobalValue(name);

                if (origValue == null)
                    return null;

                QsValue value;
                if (kind == QsCaptureContextCaptureKind.Copy)
                {
                    value = origValue.MakeCopy();
                }
                else
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue;
                }

                captures.Add(name, value);
            }

            var newContext = QsEvalContext.Make(context.StaticContext);
            newContext = newContext.SetVars(captures.ToImmutable()).SetGlobalScope(false);

            Func<Task> asyncFunc = async () =>
            {
                await foreach (var result in EvaluateStmtAsync(asyncStmt.Body, newContext))
                {
                    if (!result.HasValue) return;
                    context = result.Value;
                }
            };

            var task = asyncFunc();
            return context.AddTask(task);
        }

        internal async IAsyncEnumerable<QsEvalContext?> EvaluateForeachStmtAsync(QsForeachStmt foreachStmt, QsEvalContext context)
        {
            var (prevVars, bPrevGlobalScope) = (context.Vars, context.bGlobalScope);
            context = context.SetGlobalScope(false);

            var expResult = await expEvaluator.EvaluateExpAsync(foreachStmt.Obj, context);
            if (!expResult.HasValue) { yield return null; yield break; }
            context = expResult.Context;

            var objValue = expResult.Value as QsObjectValue;
            if (objValue == null) { yield return null; yield break; }

            var callable = objValue.GetMemberFuncs(new QsMemberFuncId("GetEnumerator"));
            if (callable == null) { yield return null; yield break; }

            var callableResult = await evaluator.EvaluateCallableAsync(callable, objValue, ImmutableArray<QsValue>.Empty, context);
            if (!callableResult.HasValue) { yield return null; yield break; }
            context = callableResult.Context;

            var enumeratorValue = callableResult.Value as QsObjectValue;
            if (enumeratorValue == null) { yield return null; yield break; }

            var moveNextFunc = enumeratorValue.GetMemberFuncs(new QsMemberFuncId("MoveNext"));
            if (moveNextFunc == null) { yield return null; yield break; }

            var getCurrentFunc = enumeratorValue.GetMemberFuncs(new QsMemberFuncId("GetCurrent"));
            if (getCurrentFunc == null) { yield return null; yield break; }

            while (true)
            {
                var moveNextResult = await evaluator.EvaluateCallableAsync(moveNextFunc, enumeratorValue, ImmutableArray<QsValue>.Empty, context);
                if (!moveNextResult.HasValue) { yield return null; yield break; }
                context = moveNextResult.Context;

                if (!(moveNextResult.Value is QsValue<bool> moveNextReturn)) { yield return null; yield break; }

                if (!moveNextReturn.Value) break;

                // GetCurrent
                var getCurrentResult = await evaluator.EvaluateCallableAsync(getCurrentFunc, enumeratorValue, ImmutableArray<QsValue>.Empty, context);
                if (!getCurrentResult.HasValue) { yield return null; yield break; }
                context = getCurrentResult.Context;

                // NOTICE: COPY
                context = context.SetValue(foreachStmt.VarName, getCurrentResult.Value.MakeCopy());

                await foreach (var result in EvaluateStmtAsync(foreachStmt.Body, context))
                {
                    if (!result.HasValue) { yield return null; yield break; }

                    context = result.Value;
                    if (context.FlowControl is QsYieldEvalFlowControl)
                    {
                        yield return context;
                        context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    }
                }

                if (context.FlowControl == QsBreakEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
                    break;
                }
                else if (context.FlowControl == QsContinueEvalFlowControl.Instance)
                {
                    context = context.SetFlowControl(QsNoneEvalFlowControl.Instance);
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
            
            yield return context.SetVars(prevVars).SetGlobalScope(bPrevGlobalScope);
        }

        async IAsyncEnumerable<QsEvalContext?> EvaluateYieldStmtAsync(QsYieldStmt yieldStmt, QsEvalContext context)
        {
            QsValue yieldValue;

            var yieldValueResult = await expEvaluator.EvaluateExpAsync(yieldStmt.Value, context);
            if (!yieldValueResult.HasValue) { yield return null; yield break; }

            yieldValue = yieldValueResult.Value;

            yield return context.SetFlowControl(new QsYieldEvalFlowControl(yieldValue));
        }
        
        internal async IAsyncEnumerable<QsEvalContext?> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: yield return await EvaluateCommandStmtAsync(cmdStmt, context); break;
                case QsVarDeclStmt varDeclStmt: yield return await EvaluateVarDeclStmtAsync(varDeclStmt, context); break;
                case QsIfStmt ifStmt:
                    await foreach (var result in EvaluateIfStmtAsync(ifStmt, context))
                        yield return result;
                    break;

                case QsForStmt forStmt:
                    await foreach (var result in EvaluateForStmtAsync(forStmt, context))
                        yield return result;
                    break;

                case QsContinueStmt continueStmt: yield return EvaluateContinueStmt(continueStmt, context); break;
                case QsBreakStmt breakStmt: yield return EvaluateBreakStmt(breakStmt, context); break;
                case QsReturnStmt returnStmt: yield return await EvaluateReturnStmtAsync(returnStmt, context); break;
                case QsBlockStmt blockStmt:
                    await foreach (var result in EvaluateBlockStmtAsync(blockStmt, context))
                        yield return result;
                    break;

                case QsExpStmt expStmt: yield return await EvaluateExpStmtAsync(expStmt, context); break;
                case QsTaskStmt taskStmt: yield return EvaluateTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt:
                    await foreach (var result in EvaluateAwaitStmtAsync(awaitStmt, context))
                        yield return result;
                    break;

                case QsAsyncStmt asyncStmt: yield return EvaluateAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt:
                    await foreach (var result in EvaluateForeachStmtAsync(foreachStmt, context))
                        yield return result;
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