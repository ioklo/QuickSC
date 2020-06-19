using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using static QuickSC.QsEvaluator;

namespace QuickSC
{
    class QsExpEvaluator
    {
        private QsEvaluator evaluator;

        public QsExpEvaluator(QsEvaluator evaluator)
        {
            this.evaluator = evaluator;
        }
        
        QsValue EvaluateIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var info = (QsIdentifierExpInfo)context.AnalyzeInfo.InfosByNode[idExp];

            return info.Storage switch
            {
                QsGlobalStorage globalStorage => context.DomainService.GetGlobalValue(globalStorage.VarId),
                QsLocalStorage localStorage => context.LocalVars[localStorage.LocalIndex]!,
                _ => throw new NotImplementedException()
            };
        }

        QsValue EvaluateBoolLiteralExp(QsBoolLiteralExp boolLiteralExp, QsEvalContext context)
        {
            return context.RuntimeModule.MakeBool(boolLiteralExp.Value);
        }

        QsValue EvaluateIntLiteralExp(QsIntLiteralExp intLiteralExp, QsEvalContext context)
        {
            return context.RuntimeModule.MakeInt(intLiteralExp.Value);
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

                        var strValue = context.RuntimeModule.GetString(result);
                        sb.Append(strValue);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            return context.RuntimeModule.MakeString(context.DomainService, sb.ToString());
        }

        async ValueTask<QsValue> EvaluateUnaryOpExpAsync(QsUnaryOpExp exp, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case QsUnaryOpKind.PostfixInc:  // i++
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);

                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        var retValue = context.RuntimeModule.MakeInt(intValue);
                        context.RuntimeModule.SetInt(operandValue, intValue + 1);

                        return retValue;
                    }

                case QsUnaryOpKind.PostfixDec:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);

                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        var retValue = context.RuntimeModule.MakeInt(intValue);
                        context.RuntimeModule.SetInt(operandValue, intValue - 1);
                        return retValue;
                    }

                case QsUnaryOpKind.LogicalNot:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var boolValue = context.RuntimeModule.GetBool(operandValue);
                        return context.RuntimeModule.MakeBool(!boolValue);
                    }

                case QsUnaryOpKind.PrefixInc:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue + 1);
                        return operandValue;
                    }

                case QsUnaryOpKind.PrefixDec:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        context.RuntimeModule.SetInt(operandValue, intValue - 1);
                        return operandValue;
                    }

                case QsUnaryOpKind.Minus:
                    {
                        var operandValue = await EvaluateExpAsync(exp.Operand, context);
                        var intValue = context.RuntimeModule.GetInt(operandValue);
                        return context.RuntimeModule.MakeInt(-intValue);
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
                        var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                        var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                        return context.RuntimeModule.MakeInt(intValue0 * intValue1);
                    }

                case QsBinaryOpKind.Divide:
                    {
                        var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                        var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                        return context.RuntimeModule.MakeInt(intValue0 / intValue1);
                    }

                case QsBinaryOpKind.Modulo:
                    {
                        var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                        var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                        return context.RuntimeModule.MakeInt(intValue0 % intValue1);
                    }

                case QsBinaryOpKind.Add:
                    {
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        // TODO: 이쪽은 operator+로 교체될 것이므로 임시로 하드코딩
                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                            var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                            return context.RuntimeModule.MakeInt(intValue0 + intValue1);
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var strValue0 = context.RuntimeModule.GetString(operandValue0);
                            var strValue1 = context.RuntimeModule.GetString(operandValue1);

                            return context.RuntimeModule.MakeString(context.DomainService, strValue0 + strValue1);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Subtract:
                    {
                        var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                        var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                        return context.RuntimeModule.MakeInt(intValue0 - intValue1);
                    }

                case QsBinaryOpKind.LessThan:
                    {
                        // TODO: 이쪽은 operator<로 교체될 것이므로 임시로 하드코딩
                        var info = (QsBinaryOpExpInfo)context.AnalyzeInfo.InfosByNode[exp];

                        if (info.Type == QsBinaryOpExpInfo.OpType.Integer)
                        {
                            var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                            var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                            return context.RuntimeModule.MakeBool(intValue0 < intValue1);
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var strValue0 = context.RuntimeModule.GetString(operandValue0);
                            var strValue1 = context.RuntimeModule.GetString(operandValue1);

                            return context.RuntimeModule.MakeBool(strValue0.CompareTo(strValue1) < 0);
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
                            var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                            var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                            return context.RuntimeModule.MakeBool(intValue0 > intValue1);
                        }
                        else if (info.Type== QsBinaryOpExpInfo.OpType.String)
                        {
                            var strValue0 = context.RuntimeModule.GetString(operandValue0);
                            var strValue1 = context.RuntimeModule.GetString(operandValue1);

                            return context.RuntimeModule.MakeBool(strValue0.CompareTo(strValue1) > 0);
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
                            var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                            var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                            return context.RuntimeModule.MakeBool(intValue0 <= intValue1);
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var strValue0 = context.RuntimeModule.GetString(operandValue0);
                            var strValue1 = context.RuntimeModule.GetString(operandValue1);

                            return context.RuntimeModule.MakeBool(strValue0.CompareTo(strValue1) <= 0);
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
                            var intValue0 = context.RuntimeModule.GetInt(operandValue0);
                            var intValue1 = context.RuntimeModule.GetInt(operandValue1);

                            return context.RuntimeModule.MakeBool(intValue0 >= intValue1);
                        }
                        else if (info.Type == QsBinaryOpExpInfo.OpType.String)
                        {
                            var strValue0 = context.RuntimeModule.GetString(operandValue0);
                            var strValue1 = context.RuntimeModule.GetString(operandValue1);

                            return context.RuntimeModule.MakeBool(strValue0.CompareTo(strValue1) >= 0);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }

                case QsBinaryOpKind.Equal:
                    return context.RuntimeModule.MakeBool(operandValue0.Equals(operandValue1));

                case QsBinaryOpKind.NotEqual:
                    return context.RuntimeModule.MakeBool(!operandValue0.Equals(operandValue1));

                case QsBinaryOpKind.Assign:
                    {
                        operandValue0.SetValue(operandValue1);
                        return operandValue0;
                    }
            }

            throw new NotImplementedException();
        }

        async ValueTask<QsValue> EvaluateCallExpAsync(QsCallExp exp, QsEvalContext context)
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

            return await evaluator.EvaluateFuncInstAsync(funcInst.bThisCall ? context.ThisValue : null, funcInst, argsBuilder.MoveToImmutable(), context);
        }
        
        QsValue EvaluateLambdaExp(QsLambdaExp exp, QsEvalContext context)
        {
            var info = (QsLambdaExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);

            return new QsFuncInstValue(new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.ThisValue : null,
                captures,
                info.LocalVarCount,
                exp.Body));
        }

        async ValueTask<QsValue> EvaluateIndexerExpAsync(QsIndexerExp exp, QsEvalContext context)
        {
            var info = (QsIndexerExpInfo)context.AnalyzeInfo.InfosByNode[exp];
            
            QsValue thisValue = await EvaluateExpAsync(exp.Object, context);
            var index = await EvaluateExpAsync(exp.Index, context);
            var funcInst = context.DomainService.GetFuncInst(info.FuncValue);

            return await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, ImmutableArray.Create(index), context);
        }

        async ValueTask<QsValue> EvaluateMemberCallExpAsync(QsMemberCallExp exp, QsEvalContext context)
        {
            var info = (QsMemberCallExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            switch (info.Kind)
            {
                case QsMemberCallExpInfo.CallKind.InstanceFuncCall instanceFuncCall:
                    {
                        QsValue thisValue = await EvaluateExpAsync(exp.Object, context);
                        var args = await EvaluateArgsAsync(exp.Args);
                        var funcInst = context.DomainService.GetFuncInst(instanceFuncCall.FuncValue);
                        return await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, args, context);
                    }

                case QsMemberCallExpInfo.CallKind.StaticFuncCall staticFuncCall:
                    {   
                        if (staticFuncCall.bEvaluateObject)
                            await EvaluateExpAsync(exp.Object, context);
                        var args = await EvaluateArgsAsync(exp.Args);
                        var funcInst = context.DomainService.GetFuncInst(staticFuncCall.FuncValue);
                        return await evaluator.EvaluateFuncInstAsync(null, funcInst, args, context);
                    }

                case QsMemberCallExpInfo.CallKind.InstanceLambdaCall instanceLambdaCall:
                    {
                        QsValue thisValue = await EvaluateExpAsync(exp.Object, context);
                        var args = await EvaluateArgsAsync(exp.Args);
                        var memberValue = thisValue.GetMemberValue(instanceLambdaCall.VarName);
                        var funcInst = ((QsFuncInstValue)memberValue).FuncInst;
                        return await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, args, context);
                    }

                case QsMemberCallExpInfo.CallKind.StaticLambdaCall staticLambdaCall:
                    {
                        if (staticLambdaCall.bEvaluateObject)
                            await EvaluateExpAsync(exp.Object, context);
                        var args = await EvaluateArgsAsync(exp.Args);                        
                        var memberValue = evaluator.GetStaticValue(staticLambdaCall.VarValue, context);
                        var funcInst = ((QsFuncInstValue)memberValue).FuncInst;
                        return await evaluator.EvaluateFuncInstAsync(null, funcInst, args, context);
                    }

                default:
                    throw new NotImplementedException();
            }

            async ValueTask<ImmutableArray<QsValue>> EvaluateArgsAsync(ImmutableArray<QsExp> exps)
            {
                var argsBuilder = ImmutableArray.CreateBuilder<QsValue>(exp.Args.Length);
                foreach (var argExp in exp.Args)
                {
                    var arg = await EvaluateExpAsync(argExp, context);
                    argsBuilder.Add(arg);
                }

                return argsBuilder.MoveToImmutable();
            }
        }

        async ValueTask<QsValue> EvaluateMemberExpAsync(QsMemberExp exp, QsEvalContext context)
        {
            // TODO: namespace가 있으면 Global(N.x의 경우) 이 될수 있다.  
            // 지금은 Instance(obj.id) / Static(Type.id)으로 나눠진다
            var info = (QsMemberExpInfo)context.AnalyzeInfo.InfosByNode[exp];

            if (info.Kind is QsMemberExpInfo.ExpKind.Instance instanceKind)
            {
                var objValue = await EvaluateExpAsync(exp.Object, context);
                return objValue.GetMemberValue(instanceKind.VarName);
            }
            else if (info.Kind is QsMemberExpInfo.ExpKind.Static staticKind)
            {
                if (staticKind.bEvaluateObject)
                    await EvaluateExpAsync(exp.Object, context);

                // object와는 별개로 static value를 가져온다
                return evaluator.GetStaticValue(staticKind.VarValue, context);                
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        async ValueTask<QsValue> EvaluateListExpAsync(QsListExp listExp, QsEvalContext context)
        {
            var info = (QsListExpInfo)context.AnalyzeInfo.InfosByNode[listExp];

            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach (var elemExp in listExp.Elems)
            {
                var elem = await EvaluateExpAsync(elemExp, context);
                elems.Add(elem);
            }

            return context.RuntimeModule.MakeList(context.DomainService, info.ElemTypeValue, elems);
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
                QsIndexerExp indexerExp => await EvaluateIndexerExpAsync(indexerExp, context),
                QsMemberCallExp memberCallExp => await EvaluateMemberCallExpAsync(memberCallExp, context),
                QsMemberExp memberExp => await EvaluateMemberExpAsync(memberExp, context),
                QsListExp listExp => await EvaluateListExpAsync(listExp, context),

                _ => throw new NotImplementedException()
            };
        }
    }
}