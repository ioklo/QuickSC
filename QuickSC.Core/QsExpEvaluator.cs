using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
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
        private IQsRuntimeModule runtimeModule;

        public QsExpEvaluator(QsEvaluator evaluator, IQsRuntimeModule runtimeModule)
        {
            this.evaluator = evaluator;
            this.runtimeModule = runtimeModule;
        }                
        
        QsValue EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            // id를 어디서 가져와야 하는지,
            var (storage, kind) = context.StoragesByExp[idExp];

            // TODO: 일단 기본적인 storage부터 구현한다. 람다는 추후에
            if (kind == QsStorageKind.Func)
                throw new NotImplementedException();

            Debug.Assert(kind == QsStorageKind.Var);

            // 전역 변수는 
            switch(storage)
            {
                case QsLocalStorage localStorage:
                    return context.GetLocalValue(idExp.Value);

                case QsGlobalStorage globalStorage:
                    return context.GlobalVars[(globalStorage.Metadata, idExp.Value)];

                case QsStaticStorage staticStorage:
                    throw new NotImplementedException();
                //    return context.GetStaticValue(staticStorage.TypeValue;

                case QsInstanceStorage instStorage:
                    return context.ThisValue.GetMemberValue(idExp.Value);

                default:
                    throw new InvalidOperationException();
            }
        }

        QsValue EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return runtimeModule.MakeBool(boolLiteralExp.Value);
        }

        QsValue EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return runtimeModule.MakeInt(intLiteralExp.Value);
        }

        internal async ValueTask<QsValue> EvaluateStringExpAsync(QsStringExp stringExp, QsEvalContext context)
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

                        var strValue = runtimeModule.GetString(result);
                        sb.Append(strValue);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return runtimeModule.MakeString(sb.ToString());
        }

        async ValueTask<QsValue> EvaluateUnaryOpExpAsync(QsUnaryOpExp exp, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);

                        var intValue = runtimeModule.GetInt(operandValue);
                        var retValue = runtimeModule.MakeInt(intValue);
                        runtimeModule.SetInt(operandValue, intValue + 1);

                        return retValue;
                    }

                case QsUnaryOpKind.PostfixDec:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);

                        var intValue = runtimeModule.GetInt(operandValue);
                        var retValue = runtimeModule.MakeInt(intValue);
                        runtimeModule.SetInt(operandValue, intValue - 1);
                        return retValue;
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var boolValue = runtimeModule.GetBool(operandValue);
                        return runtimeModule.MakeBool(!boolValue);
                    }

                case QsUnaryOpKind.PrefixInc:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var intValue = runtimeModule.GetInt(operandValue);
                        runtimeModule.SetInt(operandValue, intValue + 1);
                        return operandValue;
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var intValue = runtimeModule.GetInt(operandValue);
                        runtimeModule.SetInt(operandValue, intValue - 1);
                        return operandValue;
                    }
            }

            throw new NotImplementedException();
        }
        
        async ValueTask<QsValue> EvaluateBinaryOpExpAsync(QsBinaryOpExp exp, QsEvalContext context)
        {
            var operandValue0 = await EvaluateExpAsync(exp.Operand0, context);
            var operandValue1 = await EvaluateExpAsync(exp.Operand1, context);

            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        var intValue0 = runtimeModule.GetInt(operandValue0);
                        var intValue1 = runtimeModule.GetInt(operandValue1);

                        return runtimeModule.MakeInt(intValue0 * intValue1);
                    }

                case QsBinaryOpKind.Divide:
                    {
                        var intValue0 = runtimeModule.GetInt(operandValue0);
                        var intValue1 = runtimeModule.GetInt(operandValue1);

                        return runtimeModule.MakeInt(intValue0 / intValue1);
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        var intValue0 = runtimeModule.GetInt(operandValue0);
                        var intValue1 = runtimeModule.GetInt(operandValue1);

                        return runtimeModule.MakeInt(intValue0 % intValue1);
                    }

                case QsBinaryOpKind.Add:
                    {
                        // TODO: 이쪽은 operator+로 교체될 것이므로 임시로 런타임 타입체크
                        if (context.TypeValuesByExp[exp.Operand0] == intTypeValue &&
                            context.TypeValuesByExp[exp.Operand1] == intTypeValue)
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeInt(intValue0 + intValue1);
                        }
                        else
                        {

                        }

                        var strValue0 = runtimeModule.GetString(operandValue0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = runtimeModule.GetString(operandValue1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeString(strValue0 + strValue1), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = operandValue0.Value as QsValue<int>;
                        if (intValue0 == null) return QsEvalResult<QsValue>.Invalid;

                        var intValue1 = operandValue1.Value as QsValue<int>;
                        if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                        return new QsEvalResult<QsValue>(runtimeModule.MakeInt(intValue0.Value - intValue1.Value), context);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        var intValue0 = operandValue0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandValue1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(intValue0.Value < intValue1.Value), context);
                        }

                        var strValue0 = runtimeModule.GetString(operandValue0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = runtimeModule.GetString(operandValue1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(strValue0.CompareTo(strValue1) < 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        var intValue0 = operandValue0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandValue1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(intValue0.Value > intValue1.Value), context);
                        }

                        var strValue0 = runtimeModule.GetString(operandValue0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = runtimeModule.GetString(operandValue1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(strValue0.CompareTo(strValue1) > 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        var intValue0 = operandValue0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandValue1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(intValue0.Value <= intValue1.Value), context);
                        }

                        var strValue0 = runtimeModule.GetString(operandValue0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = runtimeModule.GetString(operandValue1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(strValue0.CompareTo(strValue1) <= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        var intValue0 = operandValue0.Value as QsValue<int>;
                        if (intValue0 != null)
                        {
                            var intValue1 = operandValue1.Value as QsValue<int>;
                            if (intValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(intValue0.Value >= intValue1.Value), context);
                        }

                        var strValue0 = runtimeModule.GetString(operandValue0.Value);
                        if (strValue0 != null)
                        {
                            var strValue1 = runtimeModule.GetString(operandValue1.Value);
                            if (strValue1 == null) return QsEvalResult<QsValue>.Invalid;

                            return new QsEvalResult<QsValue>(runtimeModule.MakeBool(strValue0.CompareTo(strValue1) >= 0), context);
                        }

                        return QsEvalResult<QsValue>.Invalid;
                    }

                case QsBinaryOpKind.Equal:
                    return new QsEvalResult<QsValue>(runtimeModule.MakeBool(operandValue0.Value == operandValue1.Value), context);

                case QsBinaryOpKind.NotEqual:
                    return new QsEvalResult<QsValue>(runtimeModule.MakeBool(operandValue0.Value != operandValue1.Value), context);

                case QsBinaryOpKind.Assign:
                    {
                        // TODO: 평가 순서가 operand1부터 해야 하지 않나
                        if (operandValue0.Value.SetLocalValue(operandValue1.Value))
                            return new QsEvalResult<QsValue>(operandValue0.Value, context);

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
                if (!context.HasLocalVar(idExp.Value))
                {
                    var func = context.GetFunc(idExp.Value);

                    if (func == null)
                        return QsEvalResult<QsCallable>.Invalid;

                    return new QsEvalResult<QsCallable>(new QsFuncCallable(func), context);
                }
            }

            if (!Eval(await EvaluateExpAsync(exp, context), ref context, out var expCallable))
                return QsEvalResult<QsCallable>.Invalid;

            // TODO: Lambda 지원 다시
            //if (expCallable! is QsObjectValue objValue && objValue.Object is QsLambdaObject lambdaObj)
            //    return new QsEvalResult<QsCallable>(lambdaObj.Callable, context);

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
            throw new NotImplementedException();

            //var captureInfo = context.StaticContext.CaptureInfosByLocation[QsCaptureInfoLocation.Make(exp)];

            //var captures = ImmutableDictionary.CreateBuilder<string, QsValue>();
            //foreach (var (name, kind) in captureInfo)
            //{
            //    var origValue = context.GetValue(name);
            //    if (origValue == null) return QsEvalResult<QsValue>.Invalid;

            //    QsValue value;
            //    if (kind == QsCaptureContextCaptureKind.Copy)
            //    {
            //        value = origValue.MakeCopy();
            //    }
            //    else
            //    {
            //        Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
            //        value = origValue;
            //    }

            //    captures.Add(name, value);
            //}

            //// QsValue<QsCallable>을 리턴한다
            //return new QsEvalResult<QsValue>(
            //    new QsObjectValue(new QsLambdaObject(new QsLambdaCallable(exp, captures.ToImmutable()))),
            //    context);
        }

        async ValueTask<QsEvalResult<QsValue>> EvaluateMemberCallExpAsync(QsMemberCallExp exp, QsEvalContext context)
        {
            // a.b (2, 3, 4)
            if (!Eval(await EvaluateExpAsync(exp.Object, context), ref context, out var thisValue))
                return QsEvalResult<QsValue>.Invalid;

            var callable = thisValue!.GetMemberFuncs(new QsMemberFuncId(exp.MemberFuncName));
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

            return new QsEvalResult<QsValue>(new QsObjectValue(runtimeModule.MakeListObject(elems)), context);
        }

        internal async ValueTask<QsValue> EvaluateExpAsync(QsExp exp, QsEvalContext context)
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