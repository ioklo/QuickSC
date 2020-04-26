using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using QuickSC.Syntax;

namespace QuickSC
{
    public struct QsEvalResult<TValue>
    {
        public static QsEvalResult<TValue> Invalid = new QsEvalResult<TValue>();

        public bool HasValue { get; }
        public TValue Value { get; }
        public QsEvalContext Context { get; }
        public QsEvalResult(TValue value, QsEvalContext context)
        {
            HasValue = true;
            Value = value;
            Context = context;
        }
    }

    // TODO: 레퍼런스용 Big Step, Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        IQsCommandProvider commandProvider;

        public QsEvaluator(IQsCommandProvider commandProvider)
        {
            this.commandProvider = commandProvider;
        }

        QsEvalResult<T> bind<T, U> (QsEvalResult<QsValue> res, Func<U, QsEvalResult<T>> f) where U : class
        {
            var v = res.Value as U;
            return v == null ? QsEvalResult<T>.Invalid : f(v);
        }

        QsEvalResult<QsValue> EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var result = context.GetValue(idExp.Value);

            // 없는 경우,
            if (result == null)
                return QsEvalResult<QsValue>.Invalid;

            // 초기화 되지 않은 경우는 QsNullValue를 머금고 리턴될 것이다
            return new QsEvalResult<QsValue>(result, context);
        }

        string? ToString(QsValue value)
        {
            if (value is QsStringValue strValue) return strValue.Value;
            if (value is QsIntValue intValue) return intValue.Value.ToString();
            if (value is QsBoolValue boolValue) return boolValue.Value ? "true" : "false";

            return null;
        }

        QsEvalResult<QsValue> EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsBoolValue(boolLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsIntValue(intLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateStringExp(QsStringExp stringExp, QsEvalContext context)
        {
            // stringExp는 element들의 concatenation
            var sb = new StringBuilder();
            foreach(var elem in stringExp.Elements)
            {
                switch (elem)
                {
                    case QsTextStringExpElement textElem:
                        sb.Append(textElem.Text);
                        break;

                    case QsExpStringExpElement expElem:
                        var result = EvaluateExp(expElem.Exp, context);
                        if (!result.HasValue)
                            return QsEvalResult<QsValue>.Invalid;

                        var strValue = ToString(result.Value);

                        if (strValue == null)
                            return QsEvalResult<QsValue>.Invalid;

                        sb.Append(strValue);
                        context = result.Context;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return new QsEvalResult<QsValue>(new QsStringValue(sb.ToString()), context);
        }

        QsEvalResult<QsValue> EvaluateUnaryOpExp(QsUnaryOpExp exp, QsEvalContext context)
        {
            var operandResult = EvaluateExp(exp.OperandExp, context);
            if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

            switch(exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        return bind<QsValue, QsIntValue>(operandResult, intValue => {
                            var retValue = new QsIntValue(intValue.Value);
                            intValue.Value++;
                            return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                        });
                    }

                case QsUnaryOpKind.PostfixDec: 
                    {
                        return bind<QsValue, QsIntValue>(operandResult, intValue => {
                            var retValue = new QsIntValue(intValue.Value);
                            intValue.Value--;
                            return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                        });
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        return bind<QsValue, QsBoolValue> (operandResult, boolValue =>
                               new QsEvalResult<QsValue>(new QsBoolValue(!boolValue.Value), operandResult.Context));
                    }

                case QsUnaryOpKind.PrefixInc: 
                    {
                        return bind<QsValue, QsIntValue>(operandResult, intValue => {
                            intValue.Value++;
                            return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                        });
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        return bind<QsValue, QsIntValue>(operandResult, intValue => {
                            intValue.Value--;
                            return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                        });
                    }
            }

            throw new NotImplementedException();
        }

        QsEvalResult<QsValue> EvaluateBinaryOpExp(QsBinaryOpExp exp, QsEvalContext context)
        {
            var operandResult0 = EvaluateExp(exp.Operand0, context);
            if (!operandResult0.HasValue) return QsEvalResult<QsValue>.Invalid;

            var operandResult1 = EvaluateExp(exp.Operand1, operandResult0.Context);
            if (!operandResult1.HasValue) return QsEvalResult<QsValue>.Invalid;

            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        return bind<QsValue, QsIntValue>(operandResult0, intValue0 =>
                               bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                               new QsEvalResult<QsValue>(new QsIntValue(intValue0.Value * intValue1.Value), operandResult1.Context)));
                    }

                case QsBinaryOpKind.Divide:
                    {
                        return bind<QsValue, QsIntValue>(operandResult0, intValue0 =>
                               bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                               new QsEvalResult<QsValue>(new QsIntValue(intValue0.Value / intValue1.Value), operandResult1.Context)));
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        return bind<QsValue, QsIntValue>(operandResult0, intValue0 =>
                               bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                               new QsEvalResult<QsValue>(new QsIntValue(intValue0.Value % intValue1.Value), operandResult1.Context)));
                    }

                case QsBinaryOpKind.Add:
                    {
                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                                   new QsEvalResult<QsValue>(new QsIntValue(intValue0.Value + intValue1.Value), operandResult1.Context));
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if( strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 =>
                                   new QsEvalResult<QsValue>(new QsStringValue(strValue0.Value + strValue1.Value), operandResult1.Context));
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        return bind<QsValue, QsIntValue>(operandResult0, intValue0 =>
                               bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                               new QsEvalResult<QsValue>(new QsIntValue(intValue0.Value - intValue1.Value), operandResult1.Context)));
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(intValue0.Value < intValue1.Value), operandResult1.Context));
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if (strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(strValue0.Value.CompareTo(strValue1.Value) < 0), operandResult1.Context));
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(intValue0.Value > intValue1.Value), operandResult1.Context));
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if (strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(strValue0.Value.CompareTo(strValue1.Value) > 0), operandResult1.Context));
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(intValue0.Value <= intValue1.Value), operandResult1.Context));
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if (strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(strValue0.Value.CompareTo(strValue1.Value) <= 0), operandResult1.Context));
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(intValue0.Value >= intValue1.Value), operandResult1.Context));
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if (strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 =>
                                   new QsEvalResult<QsValue>(new QsBoolValue(strValue0.Value.CompareTo(strValue1.Value) >= 0), operandResult1.Context));
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Equal:
                    return new QsEvalResult<QsValue>(new QsBoolValue(operandResult0.Value == operandResult1.Value), operandResult1.Context);                    

                case QsBinaryOpKind.NotEqual:
                    return new QsEvalResult<QsValue>(new QsBoolValue(operandResult0.Value != operandResult1.Value), operandResult1.Context);

                case QsBinaryOpKind.Assign:
                    {
                        // TODO: 평가 순서가 operand1부터 해야 하지 않나

                        var boolValue0 = operandResult0.Value as QsBoolValue;
                        if (boolValue0 != null)
                        {
                            return bind<QsValue, QsBoolValue>(operandResult1, boolValue1 => {
                                boolValue0.Value = boolValue1.Value;
                                return new QsEvalResult<QsValue>(boolValue0, operandResult1.Context);
                            });
                        }

                        var intValue0 = operandResult0.Value as QsIntValue;
                        if (intValue0 != null)
                        {
                            return bind<QsValue, QsIntValue>(operandResult1, intValue1 => {
                                intValue0.Value = intValue1.Value;
                                return new QsEvalResult<QsValue>(intValue0, operandResult1.Context);
                            });
                        }

                        var strValue0 = operandResult0.Value as QsStringValue;
                        if (strValue0 != null)
                        {
                            return bind<QsValue, QsStringValue>(operandResult1, strValue1 => {
                                strValue0.Value = strValue1.Value;
                                return new QsEvalResult<QsValue>(strValue0, operandResult1.Context);
                            });
                        }
                        
                        return QsEvalResult<QsValue>.Invalid;
                    }

            }

            throw new NotImplementedException();
        }

        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        QsEvalResult<QsFuncDecl> EvaluateCallExpCallable(QsCallExpCallable callable, QsEvalContext context)
        {
            // 타입 체커를 통해서 미리 계산된 func가 있는 경우,
            if (callable is QsFuncCallExpCallable funcCallable)
            {
                return new QsEvalResult<QsFuncDecl>(funcCallable.FuncDecl, context);
            }
            // TODO: 타입체커가 있으면 이 부분은 없어져야 한다
            else if (callable is QsExpCallExpCallable expCallable)            
            {                
                if (expCallable.Exp is QsIdentifierExp idExp)
                {
                    // 일단 idExp가 variable로 존재하는지 봐야 한다
                    if (!context.HasVar(idExp.Value))
                    {
                        var func = context.GetFunc(idExp.Value);
                        if (func == null)
                            return QsEvalResult<QsFuncDecl>.Invalid;

                        return new QsEvalResult<QsFuncDecl>(func, context);
                    }                    
                }

                var expCallableResult = EvaluateExp(expCallable.Exp, context);
                if (!expCallableResult.HasValue)
                    return QsEvalResult<QsFuncDecl>.Invalid;
                context = expCallableResult.Context;

                return bind<QsFuncDecl, QsCallableValue>(expCallableResult, callableValue =>
                       new QsEvalResult<QsFuncDecl>(callableValue.FuncDecl, context));
            }

            return QsEvalResult<QsFuncDecl>.Invalid;
        }

        QsEvalResult<QsValue> EvaluateCallExp(QsCallExp exp, QsEvalContext context)
        {
            var callableResult = EvaluateCallExpCallable(exp.Callable, context);
            if (!callableResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = callableResult.Context;
            var funcDecl = callableResult.Value;

            int paramIndex = 0;
            var args = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var arg in exp.Args)
            {
                var argResult = EvaluateExp(arg, context);
                if (!argResult.HasValue)
                    return QsEvalResult<QsValue>.Invalid;
                context = argResult.Context;

                args.Add(funcDecl.Params[paramIndex].Name, argResult.Value);
                paramIndex++;
            }

            // 프레임 전환 
            var prevVars = context.Vars;
            context = context.SetVars(args.ToImmutable());            
            
            var bodyResult = EvaluateStmt(funcDecl.Body, context);
            if (!bodyResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;
            context = bodyResult.Value;

            context = context.SetVars(prevVars);

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

        QsEvalResult<QsValue> EvaluateExp(QsExp exp, QsEvalContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => EvaluateIdExp(idExp, context),
                QsBoolLiteralExp boolExp => EvaluateBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => EvaluateIntLiteralExp(intExp, context),
                QsStringExp stringExp => EvaluateStringExp(stringExp, context),
                QsUnaryOpExp unaryOpExp => EvaluateUnaryOpExp(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => EvaluateBinaryOpExp(binaryOpExp, context),
                QsCallExp callExp => EvaluateCallExp(callExp, context),

                _ => throw new NotImplementedException()
            };
        }

        // TODO: CommandProvider가 Parser도 제공해야 할 것 같다
        QsEvalContext? EvaluateCommandStmt(QsCommandStmt stmt, QsEvalContext context)
        {
            foreach (var command in stmt.Commands)
            {
                var cmdResult = EvaluateStringExp(command, context);
                if (!cmdResult.HasValue) return null;
                context = cmdResult.Context;

                var cmdText = ToString(cmdResult.Value);
                if (cmdText == null) return null;
                
                commandProvider.Execute(cmdText);
            }
            return context;
        }

        QsEvalContext? EvaluateVarDeclStmt(QsVarDeclStmt stmt, QsEvalContext context)
        {
            return EvaluateVarDecl(stmt.VarDecl, context);
        }

        QsEvalContext? EvaluateVarDecl(QsVarDecl varDecl, QsEvalContext context)
        {
            foreach(var elem in varDecl.Elements)
            {
                QsValue value;
                if (elem.InitExp != null)
                {
                    var expResult = EvaluateExp(elem.InitExp, context);
                    if (!expResult.HasValue)
                        return null;

                    value = expResult.Value;
                    context = expResult.Context;
                }
                else
                {
                    value = QsNullValue.Instance;
                }

                context = context.SetValue(elem.VarName, value);
            }

            return context;
        }

        QsEvalContext? EvaluateIfStmt(QsIfStmt stmt, QsEvalContext context)
        {
            var condValue = EvaluateExp(stmt.CondExp, context);
            if (!condValue.HasValue) return null;

            var condBoolValue = condValue.Value as QsBoolValue;
            if (condBoolValue == null)
                return null;

            context = condValue.Context;

            if (condBoolValue.Value)
            {
                return EvaluateStmt(stmt.BodyStmt, context);
            }
            else
            {
                if (stmt.ElseBodyStmt != null)
                    return EvaluateStmt(stmt.ElseBodyStmt, context);
            }

            return context;
        }

        QsEvalContext? EvaluateForStmt(QsForStmt forStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            switch (forStmt.Initializer)
            {
                case QsExpForStmtInitializer expInitializer:
                    {
                        var valueResult = EvaluateExp(expInitializer.Exp, context);
                        if (!valueResult.HasValue) return null;
                        context = valueResult.Context;
                        break;
                    }
                case QsVarDeclForStmtInitializer varDeclInitializer:
                    {
                        var evalResult = EvaluateVarDecl(varDeclInitializer.VarDecl, context);
                        if (!evalResult.HasValue) return null;
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
                    var condExpResult = EvaluateExp(forStmt.CondExp, context);
                    if (!condExpResult.HasValue)
                        return null;

                    var condExpBoolValue = condExpResult.Value as QsBoolValue;
                    if (condExpBoolValue == null)
                        return null;

                    context = condExpResult.Context;
                    if (!condExpBoolValue.Value)
                        break;
                }
                
                var bodyStmtResult = EvaluateStmt(forStmt.BodyStmt, context);
                if (!bodyStmtResult.HasValue)
                    return null;

                context = bodyStmtResult.Value;

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
                    var contExpResult = EvaluateExp(forStmt.ContinueExp, context);
                    if (!contExpResult.HasValue)
                        return null;

                    context = contExpResult.Context;
                }
            }

            return context.SetVars(prevVars);
        }

        QsEvalContext? EvaluateContinueStmt(QsContinueStmt continueStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsContinueEvalFlowControl.Instance);
        }

        QsEvalContext? EvaluateBreakStmt(QsBreakStmt breakStmt, QsEvalContext context)
        {
            return context.SetFlowControl(QsBreakEvalFlowControl.Instance);
        }

        QsEvalContext? EvaluateReturnStmt(QsReturnStmt returnStmt, QsEvalContext context)
        {
            QsValue returnValue;
            if (returnStmt.Value != null)
            {
                var returnValueResult = EvaluateExp(returnStmt.Value, context);
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

        QsEvalContext? EvaluateBlockStmt(QsBlockStmt blockStmt, QsEvalContext context)
        {
            var prevVars = context.Vars;

            foreach(var stmt in blockStmt.Stmts)
            {
                var stmtResult = EvaluateStmt(stmt, context);
                if (!stmtResult.HasValue) return null;

                context = stmtResult.Value;

                if (context.FlowControl != QsNoneEvalFlowControl.Instance)
                    return context.SetVars(prevVars);
            }

            return context.SetVars(prevVars);
        }

        QsEvalContext? EvaluateExpStmt(QsExpStmt expStmt, QsEvalContext context)
        {
            var expResult = EvaluateExp(expStmt.Exp, context);
            if (!expResult.HasValue) return null;

            return expResult.Context;
        }

        // TODO: 임시 public, REPL용이 따로 있어야 할 것 같다
        public QsEvalContext? EvaluateStmt(QsStmt stmt, QsEvalContext context)
        {
            return stmt switch
            {
                QsCommandStmt cmdStmt => EvaluateCommandStmt(cmdStmt, context),
                QsVarDeclStmt varDeclStmt => EvaluateVarDeclStmt(varDeclStmt, context),
                QsIfStmt ifStmt => EvaluateIfStmt(ifStmt, context),                
                QsForStmt forStmt => EvaluateForStmt(forStmt, context),
                QsContinueStmt continueStmt => EvaluateContinueStmt(continueStmt, context),
                QsBreakStmt breakStmt => EvaluateBreakStmt(breakStmt, context),
                QsReturnStmt returnStmt => EvaluateReturnStmt(returnStmt, context),
                QsBlockStmt blockStmt => EvaluateBlockStmt(blockStmt, context),
                QsExpStmt expStmt => EvaluateExpStmt(expStmt, context),
                _ => throw new NotImplementedException()
            };
        }
        
        public QsEvalContext? EvaluateScript(QsScript script, QsEvalContext context)
        {
            // decl 부터 먼저 처리
            foreach (var elem in script.Elements)
            {
                if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    context = context.AddFunc(funcDeclElem.FuncDecl);
                }
            }

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    var result = EvaluateStmt(statementElem.Stmt, context);
                    if (!result.HasValue) return null;

                    context = result.Value;
                }
                else if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    continue;
                }
                else 
                {
                    return null;
                }
            }

            return context;
        }
    }
}
