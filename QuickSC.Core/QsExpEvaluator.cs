using QuickSC.Runtime;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;

namespace QuickSC
{
    class QsExpEvaluator
    {
        private QsEvaluator evaluator;
        private QsEvalCapturer capturer;

        public QsExpEvaluator(QsEvaluator evaluator, QsEvalCapturer capturer)
        {
            this.evaluator = evaluator;
            this.capturer = capturer;
        }

        QsEvalResult<QsValue> EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var result = context.GetValue(idExp.Value);

            if (result == null)
                result = context.GetGlobalValue(idExp.Value);

            if (result == null)
                return QsEvalResult<QsValue>.Invalid;

            return new QsEvalResult<QsValue>(result, context);
        }

        QsEvalResult<QsValue> EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<bool>(boolLiteralExp.Value), context);
        }

        QsEvalResult<QsValue> EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return new QsEvalResult<QsValue>(new QsValue<int>(intLiteralExp.Value), context);
        }

        internal async ValueTask<QsEvalResult<QsValue>> EvaluateStringExpAsync(QsStringExp stringExp, QsEvalContext context)
        {
            // stringExp는 element들의 concatenation
            var sb = new StringBuilder();
            foreach (var elem in stringExp.Elements)
            {
                switch (elem)
                {
                    case QsTextStringExpElement textElem:
                        sb.Append(textElem.Text);
                        break;

                    case QsExpStringExpElement expElem:
                        var result = await EvaluateExpAsync(expElem.Exp, context);
                        if (!result.HasValue)
                            return QsEvalResult<QsValue>.Invalid;

                        var strValue = GetString(result.Value);

                        if (strValue == null)
                            return QsEvalResult<QsValue>.Invalid;

                        sb.Append(strValue);
                        context = result.Context;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return new QsEvalResult<QsValue>(new QsObjectValue(new QsStringObject(sb.ToString())), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateUnaryOpExpAsync(QsUnaryOpExp exp, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value++;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.PostfixDec:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        var retValue = new QsValue<int>(intValue.Value);
                        intValue.Value--;
                        return new QsEvalResult<QsValue>(retValue, operandResult.Context);
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var boolValue = operandResult.Value as QsValue<bool>;
                        if (boolValue == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<bool>(!boolValue.Value), operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixInc:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value++;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        var operandResult = await EvaluateExpAsync(exp.OperandExp, context);
                        if (!operandResult.HasValue) return QsEvalResult<QsValue>.Invalid;

                        var intValue = operandResult.Value as QsValue<int>;
                        if (intValue == null) return QsEvalResult<QsValue>.Invalid;

                        intValue.Value--;
                        return new QsEvalResult<QsValue>(operandResult.Value, operandResult.Context);
                    }
            }

            throw new NotImplementedException();
        }

        private static string? GetStringValue(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj)
            {
                return strObj.Data;
            }

            return null;
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateBinaryOpExpAsync(QsBinaryOpExp exp, QsEvalContext context)
        {
            var operandResult0 = await EvaluateExpAsync(exp.Operand0, context);
            if (!operandResult0.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult0.Context;

            var operandResult1 = await EvaluateExpAsync(exp.Operand1, context);
            if (!operandResult1.HasValue) return QsEvalResult<QsValue>.Invalid;
            context = operandResult1.Context;

            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value * intValue1.Value), context);
                    }

                case QsBinaryOpKind.Divide:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value / intValue1.Value), context);
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value % intValue1.Value), context);
                    }

                case QsBinaryOpKind.Add:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value + intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsObjectValue(new QsStringObject(strValue0 + strValue1)), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandResult1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(new QsValue<int>(intValue0.Value - intValue1.Value), context);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value < intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) < 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value > intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) > 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value <= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) <= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        var intValue0 = operandResult0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandResult1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(intValue0.Value >= intValue1.Value), context);
                        }

                        var strValue0 = GetStringValue(operandResult0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = GetStringValue(operandResult1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(new QsValue<bool>(strValue0.CompareTo(strValue1) >= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Equal:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value == operandResult1.Value), context);

                case QsBinaryOpKind.NotEqual:
                    return new QsEvalResult<QsValue>(new QsValue<bool>(operandResult0.Value != operandResult1.Value), context);

                case QsBinaryOpKind.Assign:
                    {
                        // TODO: 평가 순서가 operand1부터 해야 하지 않나
                        if (operandResult0.Value.SetValue(operandResult1.Value))
                            return new QsEvalResult<QsValue>(operandResult0.Value, context);

                        return QsEvalResult<QsValue>.Invalid;
                    }

            }

            throw new NotImplementedException();
        }

        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        private async ValueTask<QsEvalResult<QsCallable>> EvaluateCallExpCallableAsync(QsExp exp, QsEvalContext context)
        {
            if (exp is QsIdentifierExp idExp)
            {
                // 일단 idExp가 variable로 존재하는지 봐야 한다
                if (!context.HasVar(idExp.Value))
                {
                    var func = context.GetFunc(idExp.Value);

                    if (func == null)
                        return QsEvalResult<QsCallable>.Invalid;

                    return new QsEvalResult<QsCallable>(new QsFuncCallable(func), context);
                }
            }

            if (!Eval(await EvaluateExpAsync(exp, context), ref context, out var expCallable))
                return QsEvalResult<QsCallable>.Invalid;

            if (expCallable! is QsObjectValue objValue && objValue.Object is QsLambdaObject lambdaObj)
                return new QsEvalResult<QsCallable>(lambdaObj.Callable, context);

            return QsEvalResult<QsCallable>.Invalid;            
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateCallExpAsync(QsCallExp exp, QsEvalContext context)
        {
            if (!Eval(await EvaluateCallExpCallableAsync(exp.Callable, context), ref context, out var callable))
                return QsEvalResult<QsValue>.Invalid;

            var args = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                if (!Eval(await EvaluateExpAsync(argExp, context), ref context, out var arg))
                    return QsEvalResult<QsValue>.Invalid;

                args.Add(arg!);
            }

            return await evaluator.EvaluateCallableAsync(callable!, QsNullValue.Instance, args.ToImmutable(), context);
        }

        QsEvalResult<QsValue> EvaluateLambdaExp(QsLambdaExp exp, QsEvalContext context)
        {
            var captureResult = capturer.CaptureLambdaExp(exp, QsCaptureContext.Make());
            if (!captureResult.HasValue)
                return QsEvalResult<QsValue>.Invalid;

            var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            foreach (var needCapture in captureResult.Value.NeedCaptures)
            {
                var name = needCapture.Key;
                var kind = needCapture.Value;

                var origValue = context.GetValue(name);
                if (origValue == null) return QsEvalResult<QsValue>.Invalid;

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

            // QsValue<QsCallable>을 리턴한다
            return new QsEvalResult<QsValue>(
                new QsObjectValue(new QsLambdaObject(new QsLambdaCallable(exp, captures.ToImmutable()))),
                context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateMemberCallExpAsync(QsMemberCallExp exp, QsEvalContext context)
        {
            // a.b (2, 3, 4)
            if (!Eval(await EvaluateExpAsync(exp.Object, context), ref context, out var thisValue))
                return QsEvalResult<QsValue>.Invalid;

            var callable = thisValue!.GetMemberFuncs(exp.MemberFuncId);
            if (callable == null) return QsEvalResult<QsValue>.Invalid;

            // 
            var args = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                if (!Eval(await EvaluateExpAsync(argExp, context), ref context, out var arg))
                    return QsEvalResult<QsValue>.Invalid;

                args.Add(arg!);
            }

            return await evaluator.EvaluateCallableAsync(callable, thisValue!, args.ToImmutable(), context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateMemberExpAsync(QsMemberExp exp, QsEvalContext context)
        {
            if (!Eval(await EvaluateExpAsync(exp.Object, context), ref context, out var thisValue))
                return QsEvalResult<QsValue>.Invalid;

            var value = thisValue!.GetMemberValue(exp.MemberName);
            if (value == null) return QsEvalResult<QsValue>.Invalid;

            return new QsEvalResult<QsValue>(value, context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateListExpAsync(QsListExp listExp, QsEvalContext context)
        {
            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach (var elemExp in listExp.Elems)
            {
                var elemResult = await EvaluateExpAsync(elemExp, context);
                if (!elemResult.HasValue) return QsEvalResult<QsValue>.Invalid;
                context = elemResult.Context;

                elems.Add(elemResult.Value);
            }

            return new QsEvalResult<QsValue>(new QsObjectValue(new QsListObject(elems)), context);
        }

        internal async ValueTask<QsEvalResult<QsValue>> EvaluateExpAsync(QsExp exp, QsEvalContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => EvaluateIdExp(idExp, context),
                QsBoolLiteralExp boolExp => EvaluateBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => EvaluateIntLiteralExp(intExp, context),
                QsStringExp stringExp => await EvaluateStringExpAsync(stringExp, context),
                QsUnaryOpExp unaryOpExp => await EvaluateUnaryOpExpAsync(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => await EvaluateBinaryOpExpAsync(binaryOpExp, context),
                QsCallExp callExp => await EvaluateCallExpAsync(callExp, context),
                QsLambdaExp lambdaExp => EvaluateLambdaExp(lambdaExp, context),
                QsMemberCallExp memberCallExp => await EvaluateMemberCallExpAsync(memberCallExp, context),
                QsMemberExp memberExp => await EvaluateMemberExpAsync(memberExp, context),
                QsListExp listExp => await EvaluateListExpAsync(listExp, context),

                _ => throw new NotImplementedException()
            };
        }
    }
}