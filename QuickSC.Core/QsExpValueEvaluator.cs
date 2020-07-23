﻿using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;
using static QuickSC.QsCollectionExtensions;

namespace QuickSC
{
    class QsExpValueEvaluator
    {
        private QsEvaluator evaluator;

        public QsExpValueEvaluator(QsEvaluator evaluator)
        {
            this.evaluator = evaluator;
        }
        
        void EvalIdExp(QsIdentifierExp idExp, QsValue result, QsEvalContext context)
        {
            var info = (QsIdentifierExpInfo)context.AnalyzeInfo.InfosByNode[idExp];

            var value = info.Storage switch
            {
                // 모듈
                QsStorage.ModuleGlobal storage => context.DomainService.GetGlobalValue(storage.VarId),

                // 스크립트
                QsStorage.PrivateGlobal storage => context.PrivateGlobalVars[storage.Index]!,

                // 함수
                QsStorage.Local storage => context.LocalVars[storage.Index]!,
                _ => throw new NotImplementedException()
            };

            result.SetValue(value);
        }

        void EvalBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsValue result, QsEvalContext context)
        {
            context.RuntimeModule.SetBool(result, boolLiteralExp.Value);
        }

        void EvalIntLiteralExp(QsIntLiteralExp intLiteralExp, QsValue result, QsEvalContext context)
        {
            context.RuntimeModule.SetInt(result, intLiteralExp.Value);
        }

        internal async ValueTask EvalStringExpAsync(QsStringExp stringExp, QsValue result, QsEvalContext context)
        {
            // TODO: 분석기에서 지역변수로 하나를 등록시켜 놓자
            var tempStr = context.RuntimeModule.MakeNullObject();

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
                        {
                            var expStringExpElementInfo = (QsExpStringExpElementInfo)context.AnalyzeInfo.InfosByNode[expElem];

                            var tempValue = evaluator.GetDefaultValue(expStringExpElementInfo.ExpTypeValue, context);
                            await EvalAsync(expElem.Exp, tempValue, context);

                            var strValue = context.RuntimeModule.GetString(tempValue!);
                            sb.Append(strValue);
                            break;
                        }

                    default:
                        throw new InvalidOperationException();
                }
            }

            context.RuntimeModule.SetString(context.DomainService, result, sb.ToString());
        }

        async ValueTask EvalUnaryOpExpAsync(QsUnaryOpExp exp, QsValue result, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandValue = evaluator.EvalValueLocExp(exp.Operand, context);

                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue + 1);
                        context.RuntimeModule.SetInt(result, intValue);
                        return;
                    }

                case QsUnaryOpKind.PostfixDec: // i--
                    {
                        var operandValue = evaluator.EvalValueLocExp(exp.Operand, context);

                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue - 1);
                        context.RuntimeModule.SetInt(result, intValue);
                        return;
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        // 같은 타입이니 result 재사용
                        await EvalAsync(exp.Operand, result, context);
                        var boolValue = context.RuntimeModule.GetBool(result);
                        context.RuntimeModule.SetBool(result, !boolValue);
                        return;
                    }

                case QsUnaryOpKind.PrefixInc: // ++i
                    {
                        var operandValue = evaluator.EvalValueLocExp(exp.Operand, context);
                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue + 1);
                        context.RuntimeModule.SetInt(result, intValue + 1);
                        return;
                    }

                case QsUnaryOpKind.PrefixDec: // --i
                    {
                        var operandValue = evaluator.EvalValueLocExp(exp.Operand, context);
                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue - 1);
                        context.RuntimeModule.SetInt(result, intValue - 1);
                        return;
                    }

                case QsUnaryOpKind.Minus: // -i
                    {
                        // 타입이 같으므로 재사용
                        await EvalAsync(exp.Operand, result, context);
                        var intValue = context.RuntimeModule.GetInt(result);
                        context.RuntimeModule.SetInt(result, -intValue);
                        return;
                    }
            }

            throw new NotImplementedException();
        }

        async ValueTask EvalBinaryOpExpAsync(QsBinaryOpExp exp, QsValue result, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case QsBinaryOpKind.Multiply:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);                        
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 * intValue1);
                        return;
                    }

                case QsBinaryOpKind.Divide:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 / intValue1);
                        return;
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 % intValue1);
                        return;
                    }

                case QsBinaryOpKind.Add:
                    {
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        // TODO: 이쪽은 operator+로 교체될 것이므로 임시로 하드코딩
                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            await EvalAsync(exp.Operand0, result, context);
                            var intValue0 = context.RuntimeModule.GetInt(result);

                            await EvalAsync(exp.Operand1, result, context);
                            var intValue1 = context.RuntimeModule.GetInt(result);

                            context.RuntimeModule.SetInt(result, intValue0 + intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            await EvalAsync(exp.Operand0, result, context);
                            var strValue0 = context.RuntimeModule.GetString(result);

                            await EvalAsync(exp.Operand1, result, context);
                            var strValue1 = context.RuntimeModule.GetString(result);

                            context.RuntimeModule.SetString(context.DomainService, result, strValue0 + strValue1);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 - intValue1);
                        return;
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        // TODO: 이쪽은 operator<로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 < intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) < 0);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.GreaterThan:
                    {
                        // TODO: 이쪽은 operator> 로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 > intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) > 0);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.LessThanOrEqual:
                    {
                        // TODO: 이쪽은 operator<=로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 <= intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) <= 0);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.GreaterThanOrEqual:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 >= intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) >= 0);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Equal:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 == intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) == 0);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.Bool)
                        {
                            var tempBool = context.RuntimeModule.MakeBool(false);

                            await EvalAsync(exp.Operand0, tempBool, context);
                            var boolValue0 = context.RuntimeModule.GetBool(tempBool);

                            await EvalAsync(exp.Operand1, tempBool, context);
                            var boolValue1 = context.RuntimeModule.GetBool(tempBool);

                            context.RuntimeModule.SetBool(result, boolValue0 == boolValue1);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.NotEqual:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            // TODO: 이것도 지역변수로 할당해야 한다
                            var tempInt = context.RuntimeModule.MakeInt(0);

                            await EvalAsync(exp.Operand0, tempInt, context);
                            var intValue0 = context.RuntimeModule.GetInt(tempInt);

                            await EvalAsync(exp.Operand1, tempInt, context);
                            var intValue1 = context.RuntimeModule.GetInt(tempInt);

                            context.RuntimeModule.SetBool(result, intValue0 != intValue1);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var tempStr = context.RuntimeModule.MakeNullObject();

                            await EvalAsync(exp.Operand0, tempStr, context);
                            var strValue0 = context.RuntimeModule.GetString(tempStr);

                            await EvalAsync(exp.Operand1, tempStr, context);
                            var strValue1 = context.RuntimeModule.GetString(tempStr);

                            context.RuntimeModule.SetBool(result, strValue0.CompareTo(strValue1) != 0);
                            return;
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.Bool)
                        {
                            var tempBool = context.RuntimeModule.MakeBool(false);

                            await EvalAsync(exp.Operand0, tempBool, context);
                            var boolValue0 = context.RuntimeModule.GetBool(tempBool);

                            await EvalAsync(exp.Operand1, tempBool, context);
                            var boolValue1 = context.RuntimeModule.GetBool(tempBool);

                            context.RuntimeModule.SetBool(result, boolValue0 != boolValue1);
                            return;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Assign:
                    {
                        var loc = evaluator.EvalValueLocExp(exp.Operand0, context);
                        await EvalAsync(exp.Operand1, loc, context);

                        result.SetValue(loc);
                        return;
                    }
            }

            throw new NotImplementedException();
        }

        async ValueTask EvalCallExpAsync(QsCallExp exp, QsValue result, QsEvalContext context)
        {
            var callExpInfo = (QsCallExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            QsFuncInst funcInst;
            if (callExpInfo.FuncValue != null)
            {
                // TODO: 1. thisFunc, (TODO: 현재 class가 없으므로 virtual staticFunc 패스)            

                // 2. globalFunc (localFunc는 없으므로 패스), or 

                // var typeInstArgs = MakeTypeInstArgs(funcValue, context.TypeEnv);
                // TODO: 일단 QsTypeInst를 Empty로 둔다 .. List때문에 문제지만 List는 내부에서 TypeInst를 안쓴다
                funcInst = context.DomainService.GetFuncInst(callExpInfo.FuncValue);
            }
            else
            {
                // TODO: 이거 여기서 해도 되는 것인지 확인
                var funcInstValue = new QsFuncInstValue();

                await EvalAsync(exp.Callable, funcInstValue, context);
                funcInst = funcInstValue.FuncInst!;
            }

            var argsBuilder = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);

            foreach(var (typeValue, argExp) in Zip(callExpInfo.ArgTypeValues, exp.Args))
            {
                // 타입을 통해서 value를 만들어 낼 수 있는가..
                var argValue = evaluator.GetDefaultValue(typeValue, context);
                argsBuilder.Add(argValue);

                await EvalAsync(argExp, argValue, context);
            }

            await evaluator.EvaluateFuncInstAsync(funcInst.bThisCall ? context.ThisValue : null, funcInst, argsBuilder.MoveToImmutable(), result, context);
        }
        
        void EvalLambdaExp(QsLambdaExp exp, QsValue result, QsEvalContext context)
        {
            var info = (QsLambdaExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);

            ((QsFuncInstValue)result).SetFuncInst(new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                exp.Body));
        }

        // a[x] => a.Func(x)
        async ValueTask EvalIndexerExpAsync(QsIndexerExp exp, QsValue result, QsEvalContext context)
        {
            var info = (QsIndexerExpInfo)context.AnalyzeInfo.InfosByNode[exp];
            var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue, context);
            var indexValue = evaluator.GetDefaultValue(info.IndexTypeValue, context);
            
            await EvalAsync(exp.Object, thisValue, context);

            await EvalAsync(exp.Index, indexValue, context);
            var funcInst = context.DomainService.GetFuncInst(info.FuncValue);

            await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, ImmutableArray.Create(indexValue), result, context);
        }

        async ValueTask EvalMemberCallExpAsync(QsMemberCallExp exp, QsValue result, QsEvalContext context)
        {
            var info = (QsMemberCallExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            switch (info)
            {
                case QsMemberCallExpInfo.InstanceFuncCall instanceFuncCall:
                    {
                        var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue!, context);

                        await EvalAsync(exp.Object, thisValue, context);
                        var args = await EvaluateArgsAsync(info.ArgTypeValues, exp.Args);
                        var funcInst = context.DomainService.GetFuncInst(instanceFuncCall.FuncValue);
                        await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, args, result, context);
                        return;
                    }

                case QsMemberCallExpInfo.StaticFuncCall staticFuncCall:
                    {
                        if (staticFuncCall.bEvaluateObject)                        
                        {
                            var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue!, context);
                            await EvalAsync(exp.Object, thisValue, context);
                        }

                        var args = await EvaluateArgsAsync(info.ArgTypeValues, exp.Args);
                        var funcInst = context.DomainService.GetFuncInst(staticFuncCall.FuncValue);
                        await evaluator.EvaluateFuncInstAsync(null, funcInst, args, result, context);
                        return;
                    }

                case QsMemberCallExpInfo.InstanceLambdaCall instanceLambdaCall:
                    {
                        var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue!, context);

                        await EvalAsync(exp.Object, thisValue, context);
                        var args = await EvaluateArgsAsync(info.ArgTypeValues, exp.Args);
                        var memberValue = evaluator.GetMemberValue(thisValue, instanceLambdaCall.VarName);
                        var funcInst = ((QsFuncInstValue)memberValue).FuncInst!;
                        await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, args, result, context);
                        return;
                    }

                case QsMemberCallExpInfo.StaticLambdaCall staticLambdaCall:
                    {
                        if (staticLambdaCall.bEvaluateObject)
                        {
                            var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue!, context);
                            await EvalAsync(exp.Object, thisValue, context);
                        }

                        var args = await EvaluateArgsAsync(info.ArgTypeValues, exp.Args);                        
                        var memberValue = evaluator.GetStaticValue(staticLambdaCall.VarValue, context);
                        var funcInst = ((QsFuncInstValue)memberValue).FuncInst!;
                        await evaluator.EvaluateFuncInstAsync(null, funcInst, args, result, context);
                        return;
                    }

                default:
                    throw new NotImplementedException();
            }

            async ValueTask<ImmutableArray<QsValue>> EvaluateArgsAsync(IEnumerable<QsTypeValue> argTypeValues, ImmutableArray<QsExp> argExps)
            {
                var argsBuilder = ImmutableArray.CreateBuilder<QsValue>(argExps.Length);

                foreach (var (typeValue, argExp) in Zip(argTypeValues, argExps))
                {
                    // 타입을 통해서 value를 만들어 낼 수 있는가..
                    var argValue = evaluator.GetDefaultValue(typeValue, context);
                    argsBuilder.Add(argValue);

                    await EvalAsync(argExp, argValue, context);
                }

                return argsBuilder.MoveToImmutable();
            }
        }

        async ValueTask EvalMemberExpAsync(QsMemberExp exp, QsValue result, QsEvalContext context)
        {
            // TODO: namespace가 있으면 Global(N.x의 경우) 이 될수 있다.  
            // 지금은 Instance(obj.id) / Static(Type.id)으로 나눠진다
            var info = (QsMemberExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            if (info is QsMemberExpInfo.Instance instanceKind)
            {
                var objValue = evaluator.GetDefaultValue(instanceKind.ObjectTypeValue, context);
                await EvalAsync(exp.Object, objValue, context);

                result.SetValue(evaluator.GetMemberValue(objValue, instanceKind.VarName));
            }
            else if (info is QsMemberExpInfo.Static staticKind)
            {
                if (staticKind.bEvaluateObject)
                {
                    var objValue = evaluator.GetDefaultValue(staticKind.ObjectTypeValue!, context);
                    await EvalAsync(exp.Object, objValue, context);
                }

                // object와는 별개로 static value를 가져온다
                result.SetValue(evaluator.GetStaticValue(staticKind.VarValue, context));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        async ValueTask EvalListExpAsync(QsListExp listExp, QsValue result, QsEvalContext context)
        {
            var info = (QsListExpInfo)context.AnalyzeInfo.InfosByNode[listExp];

            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach (var elemExp in listExp.Elems)
            {
                var elemValue = evaluator.GetDefaultValue(info.ElemTypeValue, context);
                elems.Add(elemValue);

                await EvalAsync(elemExp, elemValue, context);
            }

            context.RuntimeModule.SetList(context.DomainService, result, info.ElemTypeValue, elems);
        }

        internal async ValueTask EvalAsync(QsExp exp, QsValue result, QsEvalContext context)
        {
            switch(exp)
            {
                case QsIdentifierExp idExp: EvalIdExp(idExp, result, context); break;
                case QsBoolLiteralExp boolExp: EvalBoolLiteralExp(boolExp, result, context); break;
                case QsIntLiteralExp intExp: EvalIntLiteralExp(intExp, result, context); break;
                case QsStringExp stringExp: await EvalStringExpAsync(stringExp, result, context); break;
                case QsUnaryOpExp unaryOpExp: await EvalUnaryOpExpAsync(unaryOpExp, result, context); break;
                case QsBinaryOpExp binaryOpExp: await EvalBinaryOpExpAsync(binaryOpExp, result, context); break;
                case QsCallExp callExp: await EvalCallExpAsync(callExp, result, context); break;
                case QsLambdaExp lambdaExp: EvalLambdaExp(lambdaExp, result, context); break;
                case QsIndexerExp indexerExp: await EvalIndexerExpAsync(indexerExp, result, context); break;
                case QsMemberCallExp memberCallExp: await EvalMemberCallExpAsync(memberCallExp, result, context); break;
                case QsMemberExp memberExp: await EvalMemberExpAsync(memberExp, result, context); break;
                case QsListExp listExp: await EvalListExpAsync(listExp, result, context); break;
                default:  throw new NotImplementedException();
            }
        }
    }
}