using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            return null;
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

        QsEvalResult<QsValue> EvaluateExp(QsExp exp, QsEvalContext context)
        {
            if (exp is QsIdentifierExp idExp)
                return EvaluateIdExp(idExp, context);

            else if (exp is QsStringExp stringExp)
                return EvaluateStringExp(stringExp, context);

            return QsEvalResult<QsValue>.Invalid;
        }

        QsEvalContext? EvaluateCommandStmt(QsCommandStmt stmt, QsEvalContext context)
        {
            var nameResult = EvaluateExp(stmt.CommandExp, context);
            if (!nameResult.HasValue) return null;
            context = nameResult.Context;

            var nameStr = ToString(nameResult.Value);
            if (nameStr == null) return null;

            var argStrs = new List<string>();
            foreach(var argExp in stmt.ArgExps)
            {
                var argResult = EvaluateExp(argExp, context);
                if (!argResult.HasValue) return null;
                context = argResult.Context;

                var argStr = ToString(argResult.Value);
                if (argStr == null) return null;

                argStrs.Add(argStr);
            }

            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";

            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(nameStr);
            foreach (var argStr in argStrs)
                psi.ArgumentList.Add(argStr);

            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();

            return context;
        }

        QsEvalContext? EvaluateVarDeclStmt(QsVarDeclStmt stmt, QsEvalContext context)
        {
            foreach(var elem in stmt.Elements)
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
                    value = QsNullValue.Value;
                }

                context = context.SetValue(elem.VarName, value);
            }

            return context;
        }

        // TODO: 임시 public
        public QsEvalContext? EvaluateStmt(QsStmt stmt, QsEvalContext context)
        {
            return stmt switch
            {
                QsCommandStmt cmdStmt => EvaluateCommandStmt(cmdStmt, context),
                QsVarDeclStmt varDeclStmt => EvaluateVarDeclStmt(varDeclStmt, context),

                _ => null
            };
        }        

        public QsEvalContext? EvaluateScript(QsScript script, QsEvalContext context)
        {
            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    var result = EvaluateStmt(statementElem.Stmt, context);
                    if (!result.HasValue) return null;

                    context = result.Value;
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