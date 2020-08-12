﻿using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using static QuickSC.StaticAnalyzer.QsAnalyzer;
using static QuickSC.StaticAnalyzer.QsAnalyzer.Misc;

namespace QuickSC.StaticAnalyzer
{
    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    partial class QsExpAnalyzer
    {
        QsAnalyzer analyzer;        

        public QsExpAnalyzer(QsAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        // x
        internal bool AnalyzeIdExp(QsIdentifierExp idExp, QsTypeValue? hintTypeValue, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            var typeArgs = GetTypeValues(idExp.TypeArgs, context);

            if (!context.GetIdentifierInfo(idExp.Value, typeArgs, hintTypeValue, out var idInfo))
                return false;

            if (idInfo is IdentifierInfo.Var varIdInfo)
            {
                outTypeValue = varIdInfo.TypeValue;
                context.AddNodeInfo(idExp, new QsIdentifierExpInfo(varIdInfo.StorageInfo));
                return true;
            }
            else if (idInfo is IdentifierInfo.EnumElem enumElemInfo)
            {
                if (enumElemInfo.ElemInfo.FieldInfos.Length == 0)
                {
                    outTypeValue = enumElemInfo.EnumTypeValue;
                    // TODO: IdentifierExpInfo를 EnumElem에 맞게 분기시켜야 할 것 같다, EnumElem이 StorageInfo는 아니다
                    context.AddNodeInfo(idExp, new QsIdentifierExpInfo(QsStorageInfo.MakeEnumElem(enumElemInfo.ElemInfo.Name)));
                    return true;
                }
                else
                {
                    // TODO: Func일때 감싸기
                    throw new NotImplementedException();
                }
            }

            // TODO: Func
            return false;
        }

        internal bool AnalyzeBoolLiteralExp(QsBoolLiteralExp boolExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {   
            typeValue = analyzer.GetBoolTypeValue();
            return true;
        }

        internal bool AnalyzeIntLiteralExp(QsIntLiteralExp intExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = analyzer.GetIntTypeValue();
            return true;
        }

        internal bool AnalyzeStringExp(QsStringExp stringExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            foreach(var elem in stringExp.Elements)
                analyzer.AnalyzeStringExpElement(elem, context);

            typeValue = analyzer.GetStringTypeValue();
            return true;
        }

        internal bool AnalyzeUnaryOpExp(QsUnaryOpExp unaryOpExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            var boolTypeValue = analyzer.GetBoolTypeValue();
            var intTypeValue = analyzer.GetIntTypeValue();
            
            if (!AnalyzeExp(unaryOpExp.Operand, null, context, out var operandTypeValue))            
                return false; // AnalyzeExp에서 에러가 생겼으므로 내부에서 에러를 추가했을 것이다. 여기서는 더 추가 하지 않는다

            switch (unaryOpExp.Kind)
            {
                case QsUnaryOpKind.LogicalNot:
                    {
                        if (!analyzer.IsAssignable(boolTypeValue, operandTypeValue, context))
                        {
                            context.ErrorCollector.Add(unaryOpExp, $"{unaryOpExp.Operand}에 !를 적용할 수 없습니다. bool 타입이어야 합니다");                            
                            return false;
                        }

                        typeValue = boolTypeValue;
                        return true;
                    }
                
                case QsUnaryOpKind.PostfixInc: // e.m++ 등
                case QsUnaryOpKind.PostfixDec:
                case QsUnaryOpKind.PrefixInc:
                case QsUnaryOpKind.PrefixDec:
                    return AnalyzeUnaryAssignExp(unaryOpExp, context, out typeValue);

                case QsUnaryOpKind.Minus:
                    {
                        if (!analyzer.IsAssignable(intTypeValue, operandTypeValue, context))
                        {
                            context.ErrorCollector.Add(unaryOpExp, $"{unaryOpExp.Operand}에 -를 적용할 수 없습니다. int 타입이어야 합니다");
                            return false;
                        }

                        typeValue = intTypeValue;
                        return true;
                    }

                default:
                    context.ErrorCollector.Add(unaryOpExp, $"{operandTypeValue}를 지원하는 연산자가 없습니다");
                    return false;
            }
        }        

        

        internal bool AnalyzeBinaryOpExp(QsBinaryOpExp binaryOpExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {   
            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
                return AnalyzeBinaryAssignExp(binaryOpExp, context, out typeValue);

            typeValue = null;

            var boolTypeValue = analyzer.GetBoolTypeValue();
            var intTypeValue = analyzer.GetIntTypeValue();
            var stringTypeValue = analyzer.GetStringTypeValue();

            if (!AnalyzeExp(binaryOpExp.Operand0, null, context, out var operandTypeValue0))
                return false;

            if (!AnalyzeExp(binaryOpExp.Operand1, null, context, out var operandTypeValue1))
                return false;

            if (binaryOpExp.Kind == QsBinaryOpKind.Equal || binaryOpExp.Kind == QsBinaryOpKind.NotEqual)
            {   
                if (!EqualityComparer<QsTypeValue>.Default.Equals(operandTypeValue0, operandTypeValue1))
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}을 비교할 수 없습니다");
                    return false;
                }

                if (analyzer.IsAssignable(boolTypeValue, operandTypeValue0, context) &&
                    analyzer.IsAssignable(boolTypeValue, operandTypeValue1, context))
                {
                    context.AddNodeInfo(binaryOpExp, new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.Bool));
                }
                else if (analyzer.IsAssignable(intTypeValue, operandTypeValue0, context) &&
                    analyzer.IsAssignable(intTypeValue, operandTypeValue1, context))
                {
                    context.AddNodeInfo(binaryOpExp, new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.Integer));
                }
                else if (analyzer.IsAssignable(stringTypeValue, operandTypeValue0, context) &&
                    analyzer.IsAssignable(stringTypeValue, operandTypeValue1, context))
                {
                    context.AddNodeInfo(binaryOpExp, new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.String));
                }
                else
                {
                    context.ErrorCollector.Add(binaryOpExp, $"bool, int, string만 비교를 지원합니다");
                    return false;
                }

                typeValue = boolTypeValue;
                return true;
            }

            // TODO: 일단 하드코딩, Evaluator랑 지원하는 것들이 똑같아야 한다
            if (analyzer.IsAssignable(boolTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(boolTypeValue, operandTypeValue1, context))
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue1}은 bool 형식이어야 합니다");
                    return false;
                }

                switch (binaryOpExp.Kind)
                {
                    default:
                        context.ErrorCollector.Add(binaryOpExp, $"bool 형식에 적용할 수 있는 연산자가 아닙니다");
                        return false;
                }
            }
            else if (analyzer.IsAssignable(intTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(intTypeValue, operandTypeValue1, context))
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue1}은 int 형식이어야 합니다");
                    return false;
                }

                // 하드코딩
                context.AddNodeInfo(binaryOpExp, new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.Integer));

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Multiply:
                    case QsBinaryOpKind.Divide:
                    case QsBinaryOpKind.Modulo:
                    case QsBinaryOpKind.Add:
                    case QsBinaryOpKind.Subtract:
                        typeValue = intTypeValue;
                        return true;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        typeValue = boolTypeValue;
                        return true;

                    default:
                        context.ErrorCollector.Add(binaryOpExp, $"int 형식에 적용할 수 있는 연산자가 아닙니다");
                        return false;
                }
            }
            else if (analyzer.IsAssignable(stringTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(stringTypeValue, operandTypeValue1, context))
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue1}은 string 형식이어야 합니다");
                    return false;
                }

                // 하드코딩
                context.AddNodeInfo(binaryOpExp, new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.String));

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Add:
                        typeValue = stringTypeValue;
                        return true;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        typeValue = boolTypeValue;
                        return true;

                    default:
                        context.ErrorCollector.Add(binaryOpExp, $"string 형식에 적용할 수 있는 연산자가 아닙니다");
                        return false;
                }
            }

            context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}를 지원하는 연산자가 없습니다");
            return false;
        }

        bool AnalyzeCallableIdentifierExp(
            QsIdentifierExp exp, ImmutableArray<QsTypeValue> args, Context context,
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue.Func TypeValue)? outValue)
        {
            // 1. this 검색

            // 2. global 검색
            var funcId = QsMetaItemId.Make(exp.Value, exp.TypeArgs.Length);
            var globalFuncs = context.MetadataService.GetFuncInfos(funcId).ToImmutableArray();

            if (0 < globalFuncs.Length)
            {                
                if (1 < globalFuncs.Length)
                {
                    context.ErrorCollector.Add(exp, $"이름이 {exp.Value}인 전역 함수가 여러 개 있습니다");
                    outValue = null;
                    return false;                    
                }

                var globalFunc = globalFuncs[0];

                var typeArgs = ImmutableArray.CreateRange(exp.TypeArgs, typeArg => context.GetTypeValueByTypeExp(typeArg));

                var funcValue = new QsFuncValue(globalFunc.FuncId, QsTypeArgumentList.Make(null, typeArgs));
                var funcTypeValue = context.TypeValueService.GetTypeValue(funcValue);

                if (!analyzer.CheckParamTypes(exp, funcTypeValue.Params, args, context))
                {
                    outValue = null;
                    return false;
                }
                
                outValue = (funcValue, funcTypeValue);
                return true;
            }

            // 3. 일반 exp
            return AnalyzeCallableElseExp(exp, args, context, out outValue);
        }


        bool AnalyzeCallableElseExp(
            QsExp exp, ImmutableArray<QsTypeValue> args, Context context,
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue.Func TypeValue)? outValue)
        {
            if (!AnalyzeExp(exp, null, context, out var typeValue))
            {
                outValue = null;
                return false;
            }

            var funcTypeValue = typeValue as QsTypeValue.Func;
            if (funcTypeValue == null)
            {
                context.ErrorCollector.Add(exp, $"호출 가능한 타입이 아닙니다");
                outValue = null;
                return false;
            }

            if (!analyzer.CheckParamTypes(exp, funcTypeValue.Params, args, context))
            {
                outValue = null;
                return false;
            }

            outValue = (null, funcTypeValue);
            return true;
        }
        
        // FuncValue도 같이 리턴한다
        // CallExp(F, [1]); // F(1)
        //   -> AnalyzeCallableExp(F, [Int])
        //        -> FuncValue(
        bool AnalyzeCallableExp(
            QsExp exp, 
            ImmutableArray<QsTypeValue> args, Context context, 
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue.Func TypeValue)? outValue)
        {
            if (exp is QsIdentifierExp idExp)
                return AnalyzeCallableIdentifierExp(idExp, args, context, out outValue);
            else
                return AnalyzeCallableElseExp(exp, args, context, out outValue);
        }
        
        internal bool AnalyzeCallExp(QsCallExp exp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue) 
        {
            // 여기서 분석해야 할 것은 
            // 1. 해당 Exp가 함수인지, 변수인지, 함수라면 FuncId를 넣어준다
            // 2. Callable 인자에 맞게 잘 들어갔는지 -> 완료
            // 3. 잘 들어갔다면 리턴타입 -> 완료

            outTypeValue = null;

            if (!AnalyzeExps(exp.Args, context, out var args))
                return false;
            
            // 'f'(), 'F'(), 'GetFunc()'()
            if (!AnalyzeCallableExp(exp.Callable, args, context, out var callableInfo))
                return false;

            outTypeValue = callableInfo.Value.TypeValue.Return;
            context.AddNodeInfo(exp, new QsCallExpInfo(callableInfo.Value.FuncValue, args));
            return true;
        }
        
        internal bool AnalyzeLambdaExp(QsLambdaExp lambdaExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            if (!analyzer.AnalyzeLambda(lambdaExp.Body, lambdaExp.Params, context, out var captureInfo, out var funcTypeValue, out var localVarCount))
            {
                outTypeValue = null;
                return false;
            }

            outTypeValue = funcTypeValue;
            context.AddNodeInfo(lambdaExp, new QsLambdaExpInfo(captureInfo, localVarCount));
            return true;
        }

        bool AnalyzeIndexerExp(QsIndexerExp exp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            if (!AnalyzeExp(exp.Object, null, context, out var objTypeValue))
                return false;

            if (!AnalyzeExp(exp.Index, null, context, out var indexTypeValue))
                return false;

            // objTypeValue에 indexTypeValue를 인자로 갖고 있는 indexer가 있는지
            if (!context.TypeValueService.GetMemberFuncValue(objTypeValue, QsSpecialNames.IndexerGet, ImmutableArray<QsTypeValue>.Empty, out var funcValue))
            {
                context.ErrorCollector.Add(exp, "객체에 indexer함수가 없습니다");
                return false;
            }
            
            if (IsFuncStatic(funcValue.FuncId, context))
            {
                Debug.Fail("객체에 indexer가 있는데 Static입니다");
                return false;
            }

            var funcTypeValue = context.TypeValueService.GetTypeValue(funcValue);

            if (!analyzer.CheckParamTypes(exp, funcTypeValue.Params, ImmutableArray.Create(indexTypeValue), context))
                return false;

            context.AddNodeInfo(exp, new QsIndexerExpInfo(funcValue, objTypeValue, indexTypeValue));

            outTypeValue = funcTypeValue.Return;
            return true;
        }

        // TODO: Hint를 받을 수 있게 해야 한다
        bool AnalyzeExps(IEnumerable<QsExp> exps, Context context, out ImmutableArray<QsTypeValue> outTypeValues)
        {
            var typeValues = new List<QsTypeValue>();
            foreach (var exp in exps)
            {
                if (!AnalyzeExp(exp, null, context, out var typeValue))
                {
                    outTypeValues = ImmutableArray<QsTypeValue>.Empty;
                    return false;
                }

                typeValues.Add(typeValue);
            }

            outTypeValues = typeValues.ToImmutableArray();
            return true;
        }

        internal bool AnalyzeMemberCallExp(QsMemberCallExp exp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            var result = new MemberCallExpAnalyzer(this, exp, context).Analyze();
            if (result == null) return false;

            if (!analyzer.CheckParamTypes(exp, result.Value.TypeValue.Params, result.Value.ArgTypeValues, context))
                return false;

            context.AddNodeInfo(exp, result.Value.NodeInfo);
            outTypeValue = result.Value.TypeValue.Return;
            return true;
        }

        internal bool AnalyzeMemberExp(QsMemberExp memberExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue) 
        {
            var memberExpAnalyzer = new MemberExpAnalyzer(analyzer, memberExp, context);
            var result = memberExpAnalyzer.Analyze();

            if (result != null)
            {
                context.AddNodeInfo(memberExp, result.Value.MemberExpInfo);
                outTypeValue = result.Value.TypeValue;
                return true;
            }
            else
            {
                outTypeValue = null;
                return false;
            }
        }

        internal bool AnalyzeListExp(QsListExp listExp, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;
            QsTypeValue? curElemTypeValue = null;

            foreach (var elem in listExp.Elems)
            {
                if (!AnalyzeExp(elem, null, context, out var elemTypeValue))
                    return false;

                if (curElemTypeValue == null)
                {
                    curElemTypeValue = elemTypeValue;
                    continue;
                }

                if (!EqualityComparer<QsTypeValue>.Default.Equals(curElemTypeValue, elemTypeValue))
                {
                    // TODO: 둘의 공통 조상을 찾아야 하는지 결정을 못했다..
                    context.ErrorCollector.Add(listExp, $"원소 {elem}의 타입이 {curElemTypeValue} 가 아닙니다");
                    return false;
                }
            }

            if (curElemTypeValue == null)
            {
                context.ErrorCollector.Add(listExp, $"리스트의 타입을 결정하지 못했습니다");
                return false;
            }

            var typeInfos = context.MetadataService.GetTypeInfos(QsMetaItemId.Make("List", 1)).ToImmutableArray();            

            if (typeInfos.Length != 1)
            {
                Debug.Fail("Runtime에 적합한 리스트가 없습니다");
                return false;
            }

            typeValue = QsTypeValue.MakeNormal(typeInfos[0].TypeId, QsTypeArgumentList.Make(curElemTypeValue));
            context.AddNodeInfo(listExp, new QsListExpInfo(curElemTypeValue));
            return true;
        }

        public bool AnalyzeExp(QsExp exp, QsTypeValue? hintTypeValue, Context context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            switch(exp)
            {
                case QsIdentifierExp idExp: return AnalyzeIdExp(idExp, hintTypeValue, context, out typeValue);
                case QsBoolLiteralExp boolExp: return AnalyzeBoolLiteralExp(boolExp, context, out typeValue);
                case QsIntLiteralExp intExp: return AnalyzeIntLiteralExp(intExp, context, out typeValue);
                case QsStringExp stringExp: return AnalyzeStringExp(stringExp, context, out typeValue);
                case QsUnaryOpExp unaryOpExp: return AnalyzeUnaryOpExp(unaryOpExp, context, out typeValue);
                case QsBinaryOpExp binaryOpExp: return AnalyzeBinaryOpExp(binaryOpExp, context, out typeValue);
                case QsCallExp callExp: return AnalyzeCallExp(callExp, context, out typeValue);        
                case QsLambdaExp lambdaExp: return AnalyzeLambdaExp(lambdaExp, context, out typeValue);
                case QsIndexerExp indexerExp: return AnalyzeIndexerExp(indexerExp, context, out typeValue);
                case QsMemberCallExp memberCallExp: return AnalyzeMemberCallExp(memberCallExp, context, out typeValue);
                case QsMemberExp memberExp: return AnalyzeMemberExp(memberExp, context, out typeValue);
                case QsListExp listExp: return AnalyzeListExp(listExp, context, out typeValue);
                default: throw new NotImplementedException();
            }
        }
    }
}
