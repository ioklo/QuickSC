using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using Gum.Syntax;
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
    partial class QsExpEvaluator
    {
        private QsEvaluator evaluator;

        public QsExpEvaluator(QsEvaluator evaluator)
        {
            this.evaluator = evaluator;
        }
        
        public async ValueTask<QsValue> GetValueAsync(QsStorageInfo storageInfo, QsEvalContext context)
        {
            switch(storageInfo)
            {
                // 모듈
                case QsStorageInfo.ModuleGlobal mgs:
                    return context.DomainService.GetGlobalValue(mgs.VarId);

                // 스크립트
                case QsStorageInfo.PrivateGlobal pgs:
                    return context.GetPrivateGlobalVar(pgs.Index);

                // 함수
                case QsStorageInfo.Local ls:
                    return context.GetLocalVar(ls.Index);

                // enum값
                case QsStorageInfo.EnumElem ees:
                    return new QsEnumValue(ees.Name, Enumerable.Empty<(string, QsValue)>());

                case QsStorageInfo.StaticMember sms:
                    {
                        if (sms.ObjectInfo != null)
                        {
                            var objValue = evaluator.GetDefaultValue(sms.ObjectInfo.Value.TypeValue, context);
                            await evaluator.EvalExpAsync(sms.ObjectInfo.Value.Exp, objValue, context);
                        }

                        // object와는 별개로 static value를 가져온다
                        return context.GetStaticValue(sms.VarValue);
                    }

                case QsStorageInfo.InstanceMember ims:
                    {
                        var objValue = evaluator.GetDefaultValue(ims.ObjectTypeValue, context);
                        await evaluator.EvalExpAsync(ims.ObjectExp, objValue, context);

                        return evaluator.GetMemberValue(objValue, ims.VarName);
                    }

                default:
                    throw new NotImplementedException();
            };
        }
        
        async ValueTask EvalIdExpAsync(IdentifierExp idExp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsIdentifierExpInfo>(idExp);

            var value = await GetValueAsync(info.StorageInfo, context);

            result.SetValue(value);
        }

        void EvalBoolLiteralExp(BoolLiteralExp boolLiteralExp, QsValue result, QsEvalContext context)
        {
            context.RuntimeModule.SetBool(result, boolLiteralExp.Value);
        }

        void EvalIntLiteralExp(IntLiteralExp intLiteralExp, QsValue result, QsEvalContext context)
        {
            context.RuntimeModule.SetInt(result, intLiteralExp.Value);
        }

        internal async ValueTask EvalStringExpAsync(StringExp stringExp, QsValue result, QsEvalContext context)
        {
            // stringExp는 element들의 concatenation
            var sb = new StringBuilder();
            foreach (var elem in stringExp.Elements)
            {
                switch (elem)
                {
                    case TextStringExpElement textElem:
                        sb.Append(textElem.Text);
                        break;

                    case ExpStringExpElement expElem:
                        {
                            var expStringExpElementInfo = context.GetNodeInfo<QsExpStringExpElementInfo>(expElem);
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

        async ValueTask EvalUnaryOpExpAssignAsync(UnaryOpExp exp, QsValue result, QsEvalContext context)
        {
            async ValueTask EvalDirectAsync(QsUnaryOpExpAssignInfo.Direct directInfo)
            {
                // postfix
                if (directInfo.bReturnPrevValue)
                {
                    var oldValue = evaluator.GetDefaultValue(directInfo.ValueTypeValue, context);
                    var operatorInst = context.DomainService.GetFuncInst(directInfo.OperatorValue);

                    // universal getValue 
                    var loc = await GetValueAsync(directInfo.StorageInfo, context);

                    oldValue.SetValue(loc);

                    await evaluator.EvaluateFuncInstAsync(null, operatorInst, ImmutableArray.Create(oldValue), loc, context);

                    result.SetValue(oldValue);
                }
                else
                {
                    var oldValue = evaluator.GetDefaultValue(directInfo.ValueTypeValue, context);
                    var operatorInst = context.DomainService.GetFuncInst(directInfo.OperatorValue);

                    var loc = await GetValueAsync(directInfo.StorageInfo, context);

                    oldValue.SetValue(loc);

                    await evaluator.EvaluateFuncInstAsync(null, operatorInst, ImmutableArray.Create(oldValue), loc, context);

                    result.SetValue(loc);
                }
            }

            async ValueTask EvalCallFuncAsync(QsUnaryOpExpAssignInfo.CallFunc callFuncInfo)
            {
                QsValue? thisValue = null;

                if (callFuncInfo.ObjectExp != null)
                {
                    thisValue = evaluator.GetDefaultValue(callFuncInfo.ObjectTypeValue!, context);
                    await evaluator.EvalExpAsync(callFuncInfo.ObjectExp, thisValue, context);
                }

                var argValues = new List<QsValue>(callFuncInfo.Arguments.Length);
                foreach (var arg in callFuncInfo.Arguments)
                {
                    var argValue = evaluator.GetDefaultValue(arg.TypeValue, context);
                    await evaluator.EvalExpAsync(arg.Exp, argValue, context);
                    argValues.Add(argValue);
                }

                // postfix
                if (callFuncInfo.bReturnPrevValue)
                {
                    var v0 = evaluator.GetDefaultValue(callFuncInfo.ValueTypeValue0, context);
                    var v1 = evaluator.GetDefaultValue(callFuncInfo.ValueTypeValue1, context);
                    var getterInst = context.DomainService.GetFuncInst(callFuncInfo.Getter);
                    var operatorInst = context.DomainService.GetFuncInst(callFuncInfo.Operator);
                    var setterInst = context.DomainService.GetFuncInst(callFuncInfo.Setter);

                    // v0 = obj.GetX()
                    await evaluator.EvaluateFuncInstAsync(thisValue, getterInst, argValues, v0, context);

                    // v1 = operatorInc(v0);
                    await evaluator.EvaluateFuncInstAsync(null, operatorInst, ImmutableArray.Create(v0), v1, context);

                    // obj.SetX(v1)
                    argValues.Add(v1);
                    await evaluator.EvaluateFuncInstAsync(thisValue, setterInst, argValues, QsVoidValue.Instance, context);

                    // return v0
                    result.SetValue(v0);
                }
                else
                {
                    var v0 = evaluator.GetDefaultValue(callFuncInfo.ValueTypeValue0, context);
                    var v1 = evaluator.GetDefaultValue(callFuncInfo.ValueTypeValue1, context);
                    var getterInst = context.DomainService.GetFuncInst(callFuncInfo.Getter);
                    var operatorInst = context.DomainService.GetFuncInst(callFuncInfo.Operator);
                    var setterInst = context.DomainService.GetFuncInst(callFuncInfo.Setter);

                    // v0 = obj.GetX()
                    await evaluator.EvaluateFuncInstAsync(thisValue, getterInst, argValues, v0, context);

                    // v1 = operatorInc(v0);
                    await evaluator.EvaluateFuncInstAsync(null, operatorInst, ImmutableArray.Create(v0), v1, context);

                    // obj.SetX(v1)
                    argValues.Add(v1);
                    await evaluator.EvaluateFuncInstAsync(thisValue, setterInst, argValues, QsVoidValue.Instance, context);

                    // return v1
                    result.SetValue(v1);
                }
            }

            var info = context.GetNodeInfo<QsUnaryOpExpAssignInfo>(exp);

            if (info is QsUnaryOpExpAssignInfo.Direct directInfo)
                await EvalDirectAsync(directInfo);
            else if (info is QsUnaryOpExpAssignInfo.CallFunc callFuncInfo)
                await EvalCallFuncAsync(callFuncInfo);
            else
                throw new InvalidOperationException();
        }

        async ValueTask EvalUnaryOpExpAsync(UnaryOpExp exp, QsValue result, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case UnaryOpKind.PostfixInc:  // i++                
                case UnaryOpKind.PostfixDec: // i--
                case UnaryOpKind.PrefixInc: // ++i
                case UnaryOpKind.PrefixDec: // --i
                    await EvalUnaryOpExpAssignAsync(exp, result, context);
                    return;

                case UnaryOpKind.LogicalNot:
                    {
                        // 같은 타입이니 result 재사용
                        await EvalAsync(exp.Operand, result, context);
                        var boolValue = context.RuntimeModule.GetBool(result);
                        context.RuntimeModule.SetBool(result, !boolValue);
                        return;
                    }

                

                case UnaryOpKind.Minus: // -i
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

        private ValueTask EvalBinaryAssignExpAsync(BinaryOpExp exp, QsValue result, QsEvalContext context)
        {
            // 내부함수는 꼭 필요하지 않은 이상 한 단계 아래로 만들지 않기로

            // x = 3, e.m = 3, C.m = 3, ...
            async ValueTask EvalDirectAsync(QsBinaryOpExpAssignInfo.Direct directInfo)
            {
                var loc = await GetValueAsync(directInfo.StorageInfo, context);
                await evaluator.EvalExpAsync(exp.Operand1, loc, context);
                result.SetValue(loc);
            }

            // property, indexer
            async ValueTask EvalCallSetterAsync(QsBinaryOpExpAssignInfo.CallSetter callSetterInfo)
            {
                QsValue? thisValue = null;

                // 1. Object 부분 실행
                if (callSetterInfo.Object != null)
                {
                    thisValue = evaluator.GetDefaultValue(callSetterInfo.ObjectTypeValue!, context);
                    await evaluator.EvalExpAsync(callSetterInfo.Object, thisValue, context);
                }

                var argValues = new List<QsValue>(callSetterInfo.Arguments.Length);

                // 2. operand1 실행
                var value = evaluator.GetDefaultValue(callSetterInfo.ValueTypeValue, context);
                await evaluator.EvalExpAsync(exp.Operand1, value, context);

                // 3. set value 호출 Object.Setter(Operand1)
                foreach (var arg in callSetterInfo.Arguments)
                {
                    var argValue = evaluator.GetDefaultValue(arg.TypeValue, context);
                    await evaluator.EvalExpAsync(arg.Exp, argValue, context);
                    argValues.Add(argValue);
                }

                argValues.Add(value);

                var setterInst = context.DomainService.GetFuncInst(callSetterInfo.Setter);
                await evaluator.EvaluateFuncInstAsync(thisValue, setterInst, argValues.ToImmutableArray(), QsVoidValue.Instance, context);

                // 4. result는 operand1실행 결과
                result.SetValue(value);
            }

            // BODY
            var info = context.GetNodeInfo<QsBinaryOpExpAssignInfo>(exp);

            if (info is QsBinaryOpExpAssignInfo.Direct directInfo)
                return EvalDirectAsync(directInfo);
            else if (info is QsBinaryOpExpAssignInfo.CallSetter callSetterInfo)
                return EvalCallSetterAsync(callSetterInfo);
            else
                throw new InvalidOperationException();
        }

        async ValueTask EvalBinaryOpExpAsync(BinaryOpExp exp, QsValue result, QsEvalContext context)
        {
            switch (exp.Kind)
            {
                case BinaryOpKind.Multiply:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);                        
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 * intValue1);
                        return;
                    }

                case BinaryOpKind.Divide:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 / intValue1);
                        return;
                    }

                case BinaryOpKind.Modulo:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 % intValue1);
                        return;
                    }

                case BinaryOpKind.Add:
                    {
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.Subtract:
                    {
                        await EvalAsync(exp.Operand0, result, context);
                        var intValue0 = context.RuntimeModule.GetInt(result);

                        await EvalAsync(exp.Operand1, result, context);
                        var intValue1 = context.RuntimeModule.GetInt(result);

                        context.RuntimeModule.SetInt(result, intValue0 - intValue1);
                        return;
                    }

                case BinaryOpKind.LessThan:
                    {
                        // TODO: 이쪽은 operator<로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.GreaterThan:
                    {
                        // TODO: 이쪽은 operator> 로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.LessThanOrEqual:
                    {
                        // TODO: 이쪽은 operator<=로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.GreaterThanOrEqual:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.Equal:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.NotEqual:
                    {
                        // TODO: 이쪽은 operator>=로 교체될 것이므로 임시로 하드코딩
                        var info = context.GetNodeInfo<QsBinaryOpExpInfo>(exp);

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

                case BinaryOpKind.Assign:
                    {
                        await EvalBinaryAssignExpAsync(exp, result, context);                        
                        return;
                    }
            }

            throw new NotImplementedException();
        }

        async ValueTask EvalCallExpAsync(CallExp exp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsCallExpInfo>(exp);

            if (info is QsCallExpInfo.EnumValue enumInfo)
            {
                var args = new List<QsValue>(exp.Args.Length);

                foreach (var (typeValue, argExp) in Zip(enumInfo.ArgTypeValues, exp.Args))
                {
                    // 타입을 통해서 value를 만들어 낼 수 있는가..
                    var argValue = evaluator.GetDefaultValue(typeValue, context);
                    args.Add(argValue);

                    await EvalAsync(argExp, argValue, context);
                }

                var fields = enumInfo.ElemInfo.FieldInfos.Zip(args, (fieldInfo, arg) => (fieldInfo.Name, arg));
                ((QsEnumValue)result).SetValue(enumInfo.ElemInfo.Name, fields);
            }
            else if (info is QsCallExpInfo.Normal normalInfo)
            {
                QsFuncInst funcInst;
                if (normalInfo.FuncValue != null)
                {
                    // TODO: 1. thisFunc, (TODO: 현재 class가 없으므로 virtual staticFunc 패스)            

                    // 2. globalFunc (localFunc는 없으므로 패스), or 

                    // var typeInstArgs = MakeTypeInstArgs(funcValue, context.TypeEnv);
                    // TODO: 일단 QsTypeInst를 Empty로 둔다 .. List때문에 문제지만 List는 내부에서 TypeInst를 안쓴다
                    funcInst = context.DomainService.GetFuncInst(normalInfo.FuncValue);
                }
                else
                {
                    // TODO: 이거 여기서 해도 되는 것인지 확인
                    var funcInstValue = new QsFuncInstValue();

                    await EvalAsync(exp.Callable, funcInstValue, context);
                    funcInst = funcInstValue.FuncInst!;
                }

                var args = new List<QsValue>(exp.Args.Length);

                foreach (var (typeValue, argExp) in Zip(normalInfo.ArgTypeValues, exp.Args))
                {
                    // 타입을 통해서 value를 만들어 낼 수 있는가..
                    var argValue = evaluator.GetDefaultValue(typeValue, context);
                    args.Add(argValue);

                    await EvalAsync(argExp, argValue, context);
                }

                await evaluator.EvaluateFuncInstAsync(funcInst.bThisCall ? context.GetThisValue() : null, funcInst, args, result, context);
            }
        }
        
        void EvalLambdaExp(LambdaExp exp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsLambdaExpInfo>(exp);

            var captures = evaluator.MakeCaptures(info.CaptureInfo.Captures, context);

            ((QsFuncInstValue)result).SetFuncInst(new QsScriptFuncInst(
                null,
                false,
                info.CaptureInfo.bCaptureThis ? context.GetThisValue() : null,
                captures,
                info.LocalVarCount,
                exp.Body));
        }

        // a[x] => a.Func(x)
        async ValueTask EvalIndexerExpAsync(IndexerExp exp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsIndexerExpInfo>(exp);
            var thisValue = evaluator.GetDefaultValue(info.ObjectTypeValue, context);
            var indexValue = evaluator.GetDefaultValue(info.IndexTypeValue, context);
            
            await EvalAsync(exp.Object, thisValue, context);

            await EvalAsync(exp.Index, indexValue, context);
            var funcInst = context.DomainService.GetFuncInst(info.FuncValue);

            await evaluator.EvaluateFuncInstAsync(thisValue, funcInst, ImmutableArray.Create(indexValue), result, context);
        }

        async ValueTask EvalMemberCallExpAsync(MemberCallExp exp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsMemberCallExpInfo>(exp);

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
                        var memberValue = context.GetStaticValue(staticLambdaCall.VarValue);
                        var funcInst = ((QsFuncInstValue)memberValue).FuncInst!;
                        await evaluator.EvaluateFuncInstAsync(null, funcInst, args, result, context);
                        return;
                    }

                case QsMemberCallExpInfo.EnumValue enumValueInfo:
                    {
                        var args = await EvaluateArgsAsync(info.ArgTypeValues, exp.Args);
                        var memberValues = enumValueInfo.ElemInfo.FieldInfos.Zip(args, (fieldInfo, arg) => (fieldInfo.Name, arg));
                        ((QsEnumValue)result).SetValue(enumValueInfo.ElemInfo.Name, memberValues);
                        return;
                    }

                default:
                    throw new NotImplementedException();
            }

            async ValueTask<ImmutableArray<QsValue>> EvaluateArgsAsync(IEnumerable<QsTypeValue> argTypeValues, ImmutableArray<Exp> argExps)
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

        async ValueTask EvalMemberExpAsync(MemberExp exp, QsValue result, QsEvalContext context)
        {
            // TODO: namespace가 있으면 Global(N.x의 경우) 이 될수 있다.  
            // 지금은 Instance(obj.id) / Static(Type.id)으로 나눠진다
            var info = context.GetNodeInfo<QsMemberExpInfo>(exp);

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
                result.SetValue(context.GetStaticValue(staticKind.VarValue));
            }
            else if (info is QsMemberExpInfo.EnumElem enumElemKind)
            {
                ((QsEnumValue)result).SetValue(enumElemKind.Name, Enumerable.Empty<(string, QsValue)>());
            }
            else if (info is QsMemberExpInfo.EnumElemField enumElemField)
            {
                var objValue = evaluator.GetDefaultValue(enumElemField.ObjectTypeValue, context);
                await EvalAsync(exp.Object, objValue, context);

                result.SetValue(((QsEnumValue)objValue).GetValue(exp.MemberName));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        async ValueTask EvalListExpAsync(ListExp listExp, QsValue result, QsEvalContext context)
        {
            var info = context.GetNodeInfo<QsListExpInfo>(listExp);

            var elems = new List<QsValue>(listExp.Elems.Length);

            foreach (var elemExp in listExp.Elems)
            {
                var elemValue = evaluator.GetDefaultValue(info.ElemTypeValue, context);
                elems.Add(elemValue);

                await EvalAsync(elemExp, elemValue, context);
            }

            context.RuntimeModule.SetList(context.DomainService, result, info.ElemTypeValue, elems);
        }

        internal async ValueTask EvalAsync(Exp exp, QsValue result, QsEvalContext context)
        {
            switch(exp)
            {
                case IdentifierExp idExp: await EvalIdExpAsync(idExp, result, context); break;
                case BoolLiteralExp boolExp: EvalBoolLiteralExp(boolExp, result, context); break;
                case IntLiteralExp intExp: EvalIntLiteralExp(intExp, result, context); break;
                case StringExp stringExp: await EvalStringExpAsync(stringExp, result, context); break;
                case UnaryOpExp unaryOpExp: await EvalUnaryOpExpAsync(unaryOpExp, result, context); break;
                case BinaryOpExp binaryOpExp: await EvalBinaryOpExpAsync(binaryOpExp, result, context); break;
                case CallExp callExp: await EvalCallExpAsync(callExp, result, context); break;
                case LambdaExp lambdaExp: EvalLambdaExp(lambdaExp, result, context); break;
                case IndexerExp indexerExp: await EvalIndexerExpAsync(indexerExp, result, context); break;
                case MemberCallExp memberCallExp: await EvalMemberCallExpAsync(memberCallExp, result, context); break;
                case MemberExp memberExp: await EvalMemberExpAsync(memberExp, result, context); break;
                case ListExp listExp: await EvalListExpAsync(listExp, result, context); break;
                default:  throw new NotImplementedException();
            }
        }
    }
}