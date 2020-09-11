using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;
using Gum.CompileTime;

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
        internal async ValueTask EvaluateCommandStmtAsync(CommandStmt stmt, QsEvalContext context)
        {
            var tempStr = context.RuntimeModule.MakeNullObject();

            foreach (var command in stmt.Commands)
            {
                await evaluator.EvaluateStringExpAsync(command, tempStr, context);
                var cmdText = context.RuntimeModule.GetString(tempStr);

                await commandProvider.ExecuteAsync(cmdText);
            }
        }

        internal ValueTask EvaluateVarDeclStmtAsync(VarDeclStmt stmt, QsEvalContext context)
        {
            return evaluator.EvaluateVarDeclAsync(stmt.VarDecl, context);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateIfStmtAsync(IfStmt stmt, QsEvalContext context)
        {
            bool bTestPassed;
            if (stmt.TestType == null)
            {
                var condValue = context.RuntimeModule.MakeBool(false);
                await evaluator.EvalExpAsync(stmt.Cond, condValue, context);

                bTestPassed = context.RuntimeModule.GetBool(condValue);
            }
            else
            {
                // 분석기가 미리 계산해 놓은 TypeValue를 가져온다
                var ifStmtInfo = context.GetNodeInfo<QsIfStmtInfo>(stmt);

                if (ifStmtInfo is QsIfStmtInfo.TestEnum testEnumInfo)
                {
                    var tempEnumValue = (QsEnumValue)evaluator.GetDefaultValue(testEnumInfo.TestTargetTypeValue, context);
                    await evaluator.EvalExpAsync(stmt.Cond, tempEnumValue, context);

                    bTestPassed = (tempEnumValue.ElemName == testEnumInfo.ElemName);
                }
                else if (ifStmtInfo is QsIfStmtInfo.TestClass testClassInfo)
                {
                    var tempValue = (QsObjectValue)evaluator.GetDefaultValue(testClassInfo.TestTargetTypeValue, context);
                    await evaluator.EvalExpAsync(stmt.Cond, tempValue, context);

                    var condValueTypeValue = tempValue.GetTypeInst().GetTypeValue();

                    Debug.Assert(condValueTypeValue is TypeValue.Normal);
                    bTestPassed = evaluator.IsType((TypeValue.Normal)condValueTypeValue, testClassInfo.TestTypeValue, context);
                }
                else
                {
                    throw new InvalidOperationException();
                }
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

        internal async IAsyncEnumerable<QsValue> EvaluateForStmtAsync(ForStmt forStmt, QsEvalContext context)
        {
            var forStmtInfo = context.GetNodeInfo<QsForStmtInfo>(forStmt);
            var contValue = forStmtInfo.ContTypeValue != null ? evaluator.GetDefaultValue(forStmtInfo.ContTypeValue, context) : null;

            if (forStmt.Initializer != null)
            {
                switch (forStmt.Initializer)
                {
                    case ExpForStmtInitializer expInitializer:
                        var expInitInfo = context.GetNodeInfo<QsExpForStmtInitializerInfo>(expInitializer);
                        var value = evaluator.GetDefaultValue(expInitInfo.ExpTypeValue, context);
                        await evaluator.EvalExpAsync(expInitializer.Exp, value, context);
                        break;

                    case VarDeclForStmtInitializer varDeclInitializer:
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
                    await evaluator.EvalExpAsync(forStmt.CondExp, condValue, context);                    

                    if (!context.RuntimeModule.GetBool(condValue)) 
                        break;
                }

                await foreach (var value in EvaluateStmtAsync(forStmt.Body, context))
                    yield return value;

                var flowControl = context.GetFlowControl();

                if (flowControl == QsEvalFlowControl.Break)
                {
                    context.SetFlowControl(QsEvalFlowControl.None);
                    break;
                }
                else if (flowControl == QsEvalFlowControl.Continue)
                {
                    context.SetFlowControl(QsEvalFlowControl.None);
                }
                else if (flowControl == QsEvalFlowControl.Return)
                {
                    break;
                }
                else
                {
                    Debug.Assert(context.GetFlowControl() == QsEvalFlowControl.None);
                }

                if (forStmt.ContinueExp != null)
                {   
                    await evaluator.EvalExpAsync(forStmt.ContinueExp, contValue!, context);
                }
            }
        }

        internal void EvaluateContinueStmt(ContinueStmt continueStmt, QsEvalContext context)
        {
            context.SetFlowControl(QsEvalFlowControl.Continue);
        }

        internal void EvaluateBreakStmt(BreakStmt breakStmt, QsEvalContext context)
        {
            context.SetFlowControl(QsEvalFlowControl.Break);
        }

        internal async ValueTask EvaluateReturnStmtAsync(ReturnStmt returnStmt, QsEvalContext context)
        {
            if (returnStmt.Value != null)
            {
                var retValue = context.GetRetValue();
                await evaluator.EvalExpAsync(returnStmt.Value, retValue, context);
            }

            context.SetFlowControl(QsEvalFlowControl.Return);
        }

        internal async IAsyncEnumerable<QsValue> EvaluateBlockStmtAsync(BlockStmt blockStmt, QsEvalContext context)
        {
            foreach (var stmt in blockStmt.Stmts)
            {
                await foreach (var value in EvaluateStmtAsync(stmt, context))
                {
                    yield return value;

                    // 확실하지 않아서 걸어둔다
                    Debug.Assert(context.GetFlowControl() == QsEvalFlowControl.None);
                }

                if (context.GetFlowControl() != QsEvalFlowControl.None)
                    break;
            }            
        }

        internal async ValueTask EvaluateExpStmtAsync(ExpStmt expStmt, QsEvalContext context)
        {
            var expStmtInfo = context.GetNodeInfo<QsExpStmtInfo>(expStmt);
            var temp = evaluator.GetDefaultValue(expStmtInfo.ExpTypeValue, context);

            await evaluator.EvalExpAsync(expStmt.Exp, temp, context);
        }

        internal void EvaluateTaskStmt(TaskStmt taskStmt, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsTaskStmtInfo>(taskStmt);

            // 1. funcInst로 캡쳐
            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);
            
            var funcInst = new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.GetThisValue() : null,
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

        IAsyncEnumerable<QsValue> EvaluateAwaitStmtAsync(AwaitStmt stmt, QsEvalContext context)
        {
            async IAsyncEnumerable<QsValue> EvaluateAsync()
            {
                await foreach (var value in EvaluateStmtAsync(stmt.Body, context))
                    yield return value;

                await Task.WhenAll(context.GetTasks());
            }

            return context.ExecInNewTasks(EvaluateAsync);
        }

        internal void EvaluateAsyncStmt(AsyncStmt asyncStmt, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsAsyncStmtInfo>(asyncStmt);

            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);

            var funcInst = new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.GetThisValue() : null,
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

        internal async IAsyncEnumerable<QsValue> EvaluateForeachStmtAsync(ForeachStmt foreachStmt, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsForeachStmtInfo>(foreachStmt);

            var objValue = evaluator.GetDefaultValue(info.ObjTypeValue, context);
            var enumeratorValue = evaluator.GetDefaultValue(info.EnumeratorTypeValue, context);
            var moveNextResult = context.RuntimeModule.MakeBool(false);

            await evaluator.EvalExpAsync(foreachStmt.Obj, objValue, context);
            var getEnumeratorInst = context.DomainService.GetFuncInst(info.GetEnumeratorValue);

            await evaluator.EvaluateFuncInstAsync(objValue, getEnumeratorInst, ImmutableArray<QsValue>.Empty, enumeratorValue, context);
            var moveNextInst = context.DomainService.GetFuncInst(info.MoveNextValue);
            var getCurrentInst = context.DomainService.GetFuncInst(info.GetCurrentValue);

            var elemTypeInst = context.DomainService.GetTypeInst(info.ElemTypeValue);
            context.InitLocalVar(info.ElemLocalIndex, elemTypeInst.MakeDefaultValue());

            while (true)
            {
                await evaluator.EvaluateFuncInstAsync(enumeratorValue, moveNextInst, ImmutableArray<QsValue>.Empty, moveNextResult, context);
                if (!context.RuntimeModule.GetBool(moveNextResult)) break;

                // GetCurrent
                await evaluator.EvaluateFuncInstAsync(enumeratorValue, getCurrentInst, ImmutableArray<QsValue>.Empty, context.GetLocalVar(info.ElemLocalIndex), context);

                await foreach (var value in EvaluateStmtAsync(foreachStmt.Body, context))
                {
                    yield return value;
                }

                var flowControl = context.GetFlowControl();

                if (flowControl == QsEvalFlowControl.Break)
                {
                    context.SetFlowControl(QsEvalFlowControl.None);
                    break;
                }
                else if (flowControl == QsEvalFlowControl.Continue)
                {
                    context.SetFlowControl(QsEvalFlowControl.None);
                }
                else if (flowControl == QsEvalFlowControl.Return)
                {
                    break;
                }
                else
                {
                    Debug.Assert(flowControl == QsEvalFlowControl.None);
                }
            }
        }

        async IAsyncEnumerable<QsValue> EvaluateYieldStmtAsync(YieldStmt yieldStmt, QsEvalContext context)
        {
            await evaluator.EvalExpAsync(yieldStmt.Value, context.GetRetValue(), context);
            yield return context.GetRetValue();
        }
        
        internal async IAsyncEnumerable<QsValue> EvaluateStmtAsync(Stmt stmt, QsEvalContext context)
        {
            switch (stmt)
            {
                case CommandStmt cmdStmt: 
                    await EvaluateCommandStmtAsync(cmdStmt, context); 
                    break;

                case VarDeclStmt varDeclStmt: 
                    await EvaluateVarDeclStmtAsync(varDeclStmt, context); 
                    break;

                case IfStmt ifStmt:
                    await foreach (var value in EvaluateIfStmtAsync(ifStmt, context))
                        yield return value;
                    break;

                case ForStmt forStmt:
                    await foreach (var value in EvaluateForStmtAsync(forStmt, context))
                        yield return value;
                    break;

                case ContinueStmt continueStmt: 
                    EvaluateContinueStmt(continueStmt, context); 
                    break;

                case BreakStmt breakStmt: 
                    EvaluateBreakStmt(breakStmt, context); 
                    break;

                case ReturnStmt returnStmt: 
                    await EvaluateReturnStmtAsync(returnStmt, context); 
                    break;

                case BlockStmt blockStmt:
                    await foreach (var result in EvaluateBlockStmtAsync(blockStmt, context))
                        yield return result;
                    break;

                case BlankStmt blankStmt: break;

                case ExpStmt expStmt: 
                    await EvaluateExpStmtAsync(expStmt, context); 
                    break;

                case TaskStmt taskStmt: 
                    EvaluateTaskStmt(taskStmt, context); 
                    break;

                case AwaitStmt awaitStmt:
                    await foreach (var value in EvaluateAwaitStmtAsync(awaitStmt, context))
                        yield return value;
                    break;

                case AsyncStmt asyncStmt: 
                    EvaluateAsyncStmt(asyncStmt, context); 
                    break;

                case ForeachStmt foreachStmt:
                    await foreach (var value in EvaluateForeachStmtAsync(foreachStmt, context))
                        yield return value;
                    break;

                case YieldStmt yieldStmt:
                    await foreach (var result in EvaluateYieldStmtAsync(yieldStmt, context))
                        yield return result;
                    break;

                default: 
                    throw new NotImplementedException();
            };
        }
    }
}