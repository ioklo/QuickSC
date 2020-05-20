using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Mime;
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
                    return context.GetLocalVar(idExp.Value)!;

                case QsGlobalStorage globalStorage:
                    return context.GlobalVars[(globalStorage.Metadata, idExp.Value)]!;

                case QsStaticStorage staticStorage:
                    throw new NotImplementedException();
                //    return context.GetStaticValue(staticStorage.TypeValue;

                case QsInstanceStorage instStorage:
                    return context.ThisValue!.GetMemberValue(idExp.Value);

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

        bool IsType(QsTypeValue typeValue, string name, QsEvalContext context)
        {
            if (!runtimeModule.GetGlobalType(name, 0, out var type))
                return false;

            return EqualityComparer<QsTypeValue>.Default.Equals(typeValue, new QsNormalTypeValue(null, type.TypeId));
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
                        if (IsType(context.TypeValuesByExp[exp.Operand0], "int", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "int", context))
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeInt(intValue0 + intValue1);
                        }
                        else if (IsType(context.TypeValuesByExp[exp.Operand0], "string", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "string", context))
                        {
                            var strValue0 = runtimeModule.GetString(operandValue0);
                            var strValue1 = runtimeModule.GetString(operandValue1);

                            return runtimeModule.MakeString(strValue0 + strValue1);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = runtimeModule.GetInt(operandValue0);
                        var intValue1 = runtimeModule.GetInt(operandValue1);

                        return runtimeModule.MakeInt(intValue0 - intValue1);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        // TODO: 이쪽은 operator<로 교체될 것이므로 임시로 런타임 타입체크
                        if (IsType(context.TypeValuesByExp[exp.Operand0], "int", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "int", context))
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeBool(intValue0 < intValue1);
                        }
                        else if (IsType(context.TypeValuesByExp[exp.Operand0], "string", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "string", context))
                        {
                            var strValue0 = runtimeModule.GetString(operandValue0);
                            var strValue1 = runtimeModule.GetString(operandValue1);

                            return runtimeModule.MakeBool(strValue0.CompareTo(strValue1) < 0);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        // TODO: 이쪽은 operator>로 교체될 것이므로 임시로 런타임 타입체크
                        if (IsType(context.TypeValuesByExp[exp.Operand0], "int", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "int", context))
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeBool(intValue0 > intValue1);
                        }
                        else if (IsType(context.TypeValuesByExp[exp.Operand0], "string", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "string", context))
                        {
                            var strValue0 = runtimeModule.GetString(operandValue0);
                            var strValue1 = runtimeModule.GetString(operandValue1);

                            return runtimeModule.MakeBool(strValue0.CompareTo(strValue1) > 0);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        // TODO: 이쪽은 operator<= 로 교체될 것이므로 임시로 런타임 타입체크
                        if (IsType(context.TypeValuesByExp[exp.Operand0], "int", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "int", context))
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeBool(intValue0 <= intValue1);
                        }
                        else if (IsType(context.TypeValuesByExp[exp.Operand0], "string", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "string", context))
                        {
                            var strValue0 = runtimeModule.GetString(operandValue0);
                            var strValue1 = runtimeModule.GetString(operandValue1);

                            return runtimeModule.MakeBool(strValue0.CompareTo(strValue1) <= 0);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        // TODO: 이쪽은 operator>= 로 교체될 것이므로 임시로 런타임 타입체크
                        if (IsType(context.TypeValuesByExp[exp.Operand0], "int", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "int", context))
                        {
                            var intValue0 = runtimeModule.GetInt(operandValue0);
                            var intValue1 = runtimeModule.GetInt(operandValue1);

                            return runtimeModule.MakeBool(intValue0 >= intValue1);
                        }
                        else if (IsType(context.TypeValuesByExp[exp.Operand0], "string", context) &&
                            IsType(context.TypeValuesByExp[exp.Operand1], "string", context))
                        {
                            var strValue0 = runtimeModule.GetString(operandValue0);
                            var strValue1 = runtimeModule.GetString(operandValue1);

                            return runtimeModule.MakeBool(strValue0.CompareTo(strValue1) >= 0);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Equal:
                    return runtimeModule.MakeBool(operandValue0.Equals(operandValue1));

                case QsBinaryOpKind.NotEqual:
                    return runtimeModule.MakeBool(!operandValue0.Equals(operandValue1));

                case QsBinaryOpKind.Assign:
                    {
                        operandValue0.SetValue(operandValue1);
                        return operandValue0;
                    }
            }

            throw new NotImplementedException();
        }

        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다
        //private async ValueTask<QsCallable> EvaluateCallExpCallableAsync(QsExp exp, QsEvalContext context)
        //{
        //    if (exp is QsIdentifierExp idExp)
        //    {
        //        // 일단 idExp가 variable로 존재하는지 봐야 한다
        //        if (!context.HasLocalVar(idExp.Value))
        //        {
        //            var func = context.GetFunc(idExp.Value);

        //            if (func == null)
        //                return QsEvalResult<QsCallable>.Invalid;

        //            return new QsEvalResult<QsCallable>(new QsFuncCallable(func), context);
        //        }
        //    }

        //    if (!Eval(await EvaluateExpAsync(exp, context), ref context, out var expCallable))
        //        return QsEvalResult<QsCallable>.Invalid;

        //    // TODO: Lambda 지원 다시
        //    //if (expCallable! is QsObjectValue objValue && objValue.Object is QsLambdaObject lambdaObj)
        //    //    return new QsEvalResult<QsCallable>(lambdaObj.Callable, context);

        //    return QsEvalResult<QsCallable>.Invalid;            
        //}
        
        
        async ValueTask<QsValue> EvaluateCallExpAsync(QsCallExp exp, QsEvalContext context)
        {
            QsFuncInst funcInst;

            if (context.FuncValuesByExp.TryGetValue(exp, out var funcValue))
            {
                // TODO: 1. thisFunc, (TODO: 현재 class가 없으므로 virtual staticFunc 패스)            

                // 2. globalFunc (localFunc는 없으므로 패스), or 
                funcInst = evaluator.GetFuncInst(funcValue, context);
            }
            else
            {
                var value = await EvaluateExpAsync(exp.Callable, context);
                var funcInstValue = (QsFuncInstValue)value;
                funcInst = funcInstValue.FuncInst;
            }

            var argsBuilder = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                var arg = await EvaluateExpAsync(argExp, context);
                argsBuilder.Add(arg);
            }
            var args = argsBuilder.MoveToImmutable();

            return await evaluator.EvaluateFuncInstAsync(context.ThisValue, funcInst, args, context);
        }

        // 여기서 직접 만들어야 한다

        QsValue EvaluateLambdaExp(QsLambdaExp exp, QsEvalContext context)
        {
            // TODO: global변수, static변수는 캡쳐하지 않는다. 오직 local만 캡쳐한다
            var captureInfo = context.CaptureInfosByLocation[QsCaptureInfoLocation.Make(exp)];
            var captures = evaluator.MakeCaptures(captureInfo, context);
            
            return new QsFuncInstValue(new QsScriptFuncInst(
                false,
                false, 
                captureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                ImmutableArray.CreateRange(exp.Params, param => param.Name),
                exp.Body));
        }
        
        async ValueTask<QsValue> EvaluateMemberCallExpAsync(QsMemberCallExp exp, QsEvalContext context)
        {
            // a.b (2, 3, 4), 
            var thisValue = await EvaluateExpAsync(exp.Object, context);

            var argsBuilder = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
            foreach (var argExp in exp.Args)
            {
                var arg = await EvaluateExpAsync(argExp, context);
                argsBuilder.Add(arg);
            }
            var args = argsBuilder.MoveToImmutable();

            // 1. a.b(2, 3, 4)가 A.b(this a, 2, 3, 4); 인 경우            
            QsFuncInst funcInst;
            if (context.FuncValuesByExp.TryGetValue(exp, out var funcValue))
            {
                funcInst = evaluator.GetFuncInst(thisValue, funcValue, context);
            }
            // 2. a.b(2, 3, 4)가 (a.b) (2, 3, 4)인 경우 (2, 3, 4)
            else
            {
                funcInst = ((QsFuncInstValue)thisValue).FuncInst;
            }

            return await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, args, context);

            
            //var callable = thisValue.GetMemberFuncs(new QsMemberFuncId(exp.MemberFuncName));
            //if (callable == null) return QsEvalResult<QsValue>.Invalid;
            // return await evaluator.EvaluateCallableAsync(callable, thisValue!, args.ToImmutable(), context);
        }

        async ValueTask<QsValue> EvaluateMemberExpAsync(QsMemberExp exp, QsEvalContext context)
        {
            // TODO: namespace가 있으면 MemberExp 자체가 Global(N.x의 경우), Static(N.C.x의 경우) 이 될수 있다.  
            //if (context.StaticStoragesByMemberExp.TryGetValue(exp, out var staticStorage))
            //{
            //    // return context.GetStaticValue(staticStorage.TypeValue;
            //    throw new NotImplementedException();
            //}

            var thisValue = await EvaluateExpAsync(exp.Object, context);
            return thisValue.GetMemberValue(exp.MemberName);
        }

        async ValueTask<QsValue> EvaluateListExpAsync(QsListExp listExp, QsEvalContext context)
        {
            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach (var elemExp in listExp.Elems)
            {
                var elem = await EvaluateExpAsync(elemExp, context);
                elems.Add(elem);
            }

            return runtimeModule.MakeList(elems);
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