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

        public QsStmtEvaluator(QsEvaluator evaluator, IQsCommandProvider commandProvider)
        {
            this.evaluator = evaluator;
            this.commandProvider = commandProvider;
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        internal async ValueTask EvaluateCommandStmtAsync(QsCommandStmt stmt, QsEvalContext context)
        {
            var tempStr = context.RuntimeModule.MakeNullObject();

            foreach (var command in stmt.Commands)
            {
                await evaluator.EvaluateStringExpAsync(command, tempStr, context);
                var cmdText = context.RuntimeModule.GetString(tempStr);

                await commandProvider.ExecuteAsync(cmdText);
            }
        }

        internal ValueTask EvaluateVarDeclStmtAsync(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return evaluator.EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateIfStmtAsync(QsIfStmt stmt, QsEvalContext context)
        {
            bool bTestPassed;
            if (stmt.TestType == null)
            {
                var condValue = context.RuntimeModule.MakeBool(false);
                await evaluator.EvaluateExpAsync(stmt.Cond, condValue, context);

                bTestPassed = context.RuntimeModule.GetBool(condValue);
            }
            else
            {
                // 분석기가 미리 계산해 놓은 TypeValue를 가져온다
                var ifStmtInfo = (QsIfStmtInfo)context.AnalyzeInfo.InfosByNode[stmt];

                var testObjValue = context.RuntimeModule.MakeNullObject();
                await evaluator.EvaluateExpAsync(stmt.Cond, testObjValue, context);

                var condValueTypeValue = testObjValue.GetTypeInst().GetTypeValue();

                Debug.Assert(condValueTypeValue is QsTypeValue_Normal);
                Debug.Assert(ifStmtInfo.TestTypeValue is QsTypeValue_Normal);
                bTestPassed = evaluator.IsType((QsTypeValue_Normal)condValueTypeValue, (QsTypeValue_Normal)ifStmtInfo.TestTypeValue, context);
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
            var forStmtInfo = (QsForStmtInfo)context.AnalyzeInfo.InfosByNode[forStmt];
            var contValue = forStmtInfo.ContTypeValue != null ? evaluator.GetDefaultValue(forStmtInfo.ContTypeValue, context) : null;

            if (forStmt.Initializer != null)
            {
                switch (forStmt.Initializer)
                {
                    case QsExpForStmtInitializer expInitializer:
                        var expInitInfo = (QsExpForStmtInitializerInfo)context.AnalyzeInfo.InfosByNode[expInitializer];
                        var value = evaluator.GetDefaultValue(expInitInfo.ExpTypeValue, context);
                        await evaluator.EvaluateExpAsync(expInitializer.Exp, value, context);
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
                    var condValue = context.RuntimeModule.MakeBool(false);
                    await evaluator.EvaluateExpAsync(forStmt.CondExp, condValue, context);                    

                    if (!context.RuntimeModule.GetBool(condValue)) 
                        break;
                }

                await foreach (var value in EvaluateStmtAsync(forStmt.Body, context))
                    yield return value;

                if (context.FlowControl == QsEvalFlowControl.Break)
                {
                    context.FlowControl = QsEvalFlowControl.None;
                    break;
                }
                else if (context.FlowControl == QsEvalFlowControl.Continue)
                {
                    context.FlowControl = QsEvalFlowControl.None;
                }
                else if (context.FlowControl == QsEvalFlowControl.Return)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsEvalFlowControl.None);
                }

                if (forStmt.ContinueExp != null)
                {   
                    await evaluator.EvaluateExpAsync(forStmt.ContinueExp, contValue!, context);
                }
            }
        }

        internal void EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            context.FlowControl = QsEvalFlowControl.Continue;
        }

        internal void EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            context.FlowControl = QsEvalFlowControl.Break;
        }

        internal async ValueTask EvaluateReturnStmtAsync(QsReturnStmt returnStmt, QsEvalContext context)
        {
            if (returnStmt.Value != null)
                await evaluator.EvaluateExpAsync(returnStmt.Value, context.RetValue!, context);

            context.FlowControl = QsEvalFlowControl.Return;
        }

        internal async IAsyncEnumerable<QsValue> EvaluateBlockStmtAsync(QsBlockStmt blockStmt, QsEvalContext context)
        {
            foreach (var stmt in blockStmt.Stmts)
            {
                await foreach (var value in EvaluateStmtAsync(stmt, context))
                {
                    yield return value;

                    // 확실하지 않아서 걸어둔다
                    Debug.Assert(context.FlowControl == QsEvalFlowControl.None);
                }

                if (context.FlowControl != QsEvalFlowControl.None)
                    break;
            }            
        }

        internal async ValueTask EvaluateExpStmtAsync(QsExpStmt expStmt, QsEvalContext context)
        {
            var expStmtInfo = (QsExpStmtInfo)context.AnalyzeInfo.InfosByNode[expStmt];
            var temp = evaluator.GetDefaultValue(expStmtInfo.ExpTypeValue, context);

            await evaluator.EvaluateExpAsync(expStmt.Exp, temp, context);
        }

        internal void EvaluateTaskStmt(QsTaskStmt taskStmt, QsEvalContext context)
        {
            var info = (QsTaskStmtInfo)context.AnalyzeInfo.InfosByNode[taskStmt];

            // 1. funcInst로 캡쳐
            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);
            
            var funcInst = new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                taskStmt.Body);

            var newContext = new QsEvalContext(context, new QsValue?[0], QsEvalFlowControl.None, ImmutableArray<Task>.Empty, null, QsVoidValue.Instance);

            // 2. 그 funcInst를 바로 실행하기
            var task = Task.Run(async () =>
            {
                await evaluator.EvaluateFuncInstAsync(null, funcInst, ImmutableArray<QsValue>.Empty, QsVoidValue.Instance, newContext);
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
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                asyncStmt.Body);

            var newContext = new QsEvalContext(context, new QsValue?[0], QsEvalFlowControl.None, ImmutableArray<Task>.Empty, null, QsVoidValue.Instance);

            Func<Task> asyncFunc = async () =>
            {
                await evaluator.EvaluateFuncInstAsync(null, funcInst, ImmutableArray<QsValue>.Empty, QsVoidValue.Instance, newContext);
            };

            var task = asyncFunc();
            context.AddTask(task);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateForeachStmtAsync(QsForeachStmt foreachStmt, QsEvalContext context)
        {
            var info = (QsForeachStmtInfo)context.AnalyzeInfo.InfosByNode[foreachStmt];

            var objValue = evaluator.GetDefaultValue(info.ObjTypeValue, context);
            var enumeratorValue = evaluator.GetDefaultValue(info.EnumeratorTypeValue, context);
            var moveNextResult = context.RuntimeModule.MakeBool(false);

            await evaluator.EvaluateExpAsync(foreachStmt.Obj, objValue, context);
            var getEnumeratorInst = context.DomainService.GetFuncInst(info.GetEnumeratorValue);

            await evaluator.EvaluateFuncInstAsync(objValue, getEnumeratorInst, ImmutableArray<QsValue>.Empty, enumeratorValue, context);
            var moveNextInst = context.DomainService.GetFuncInst(info.MoveNextValue);
            var getCurrentInst = context.DomainService.GetFuncInst(info.GetCurrentValue);

            var elemTypeInst = context.DomainService.GetTypeInst(info.ElemTypeValue);
            context.LocalVars[info.ElemLocalIndex] = elemTypeInst.MakeDefaultValue();

            while (true)
            {
                await evaluator.EvaluateFuncInstAsync(enumeratorValue, moveNextInst, ImmutableArray<QsValue>.Empty, moveNextResult, context);
                if (!context.RuntimeModule.GetBool(moveNextResult)) break;

                // GetCurrent
                await evaluator.EvaluateFuncInstAsync(enumeratorValue, getCurrentInst, ImmutableArray<QsValue>.Empty, context.LocalVars[info.ElemLocalIndex]!, context);

                await foreach (var value in EvaluateStmtAsync(foreachStmt.Body, context))
                {
                    yield return value;
                }

                if (context.FlowControl == QsEvalFlowControl.Break)
                {
                    context.FlowControl = QsEvalFlowControl.None;
                    break;
                }
                else if (context.FlowControl == QsEvalFlowControl.Continue)
                {
                    context.FlowControl = QsEvalFlowControl.None;
                }
                else if (context.FlowControl == QsEvalFlowControl.Return)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsEvalFlowControl.None);
                }
            }
        }

        async IAsyncEnumerable<QsValue> EvaluateYieldStmtAsync(QsYieldStmt yieldStmt, QsEvalContext context)
        {
            await evaluator.EvaluateExpAsync(yieldStmt.Value, context.RetValue!, context);
            yield return context.RetValue!;
        }
        
        internal async IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: 
                    await EvaluateCommandStmtAsync(cmdStmt, context); 
                    break;

                case QsVarDeclStmt varDeclStmt: 
                    await EvaluateVarDeclStmtAsync(varDeclStmt, context); 
                    break;

                case QsIfStmt ifStmt:
                    await foreach (var value in EvaluateIfStmtAsync(ifStmt, context))
                        yield return value;
                    break;

                case QsForStmt forStmt:
                    await foreach (var value in EvaluateForStmtAsync(forStmt, context))
                        yield return value;
                    break;

                case QsContinueStmt continueStmt: 
                    EvaluateContinueStmt(continueStmt, context); 
                    break;

                case QsBreakStmt breakStmt: 
                    EvaluateBreakStmt(breakStmt, context); 
                    break;

                case QsReturnStmt returnStmt: 
                    await EvaluateReturnStmtAsync(returnStmt, context); 
                    break;

                case QsBlockStmt blockStmt:
                    await foreach (var result in EvaluateBlockStmtAsync(blockStmt, context))
                        yield return result;
                    break;

                case QsBlankStmt blankStmt: break;

                case QsExpStmt expStmt: 
                    await EvaluateExpStmtAsync(expStmt, context); 
                    break;

                case QsTaskStmt taskStmt: 
                    EvaluateTaskStmt(taskStmt, context); 
                    break;

                case QsAwaitStmt awaitStmt:
                    await foreach (var value in EvaluateAwaitStmtAsync(awaitStmt, context))
                        yield return value;
                    break;

                case QsAsyncStmt asyncStmt: 
                    EvaluateAsyncStmt(asyncStmt, context); 
                    break;

                case QsForeachStmt foreachStmt:
                    await foreach (var value in EvaluateForeachStmtAsync(foreachStmt, context))
                        yield return value;
                    break;

                case QsYieldStmt yieldStmt:
                    await foreach (var result in EvaluateYieldStmtAsync(yieldStmt, context))
                        yield return result;
                    break;

                default: 
                    throw new NotImplementedException();
            };
        }
    }
}