using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    class QsExpAnalyzer
    {
        QsAnalyzer analyzer;
        QsTypeValueService typeValueService;

        public QsExpAnalyzer(QsAnalyzer analyzer, QsTypeValueService typeValueService)
        {
            this.analyzer = analyzer;
            this.typeValueService = typeValueService;
        }

        internal bool AnalyzeIdExpAsType(QsIdentifierExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            var typeArgs = ImmutableArray.CreateBuilder<QsTypeValue>(exp.TypeArgs.Length);
            foreach (var typeArg in exp.TypeArgs)
                typeArgs.Add(context.TypeValuesByTypeExp[typeArg]);

            return analyzer.GetGlobalTypeValue(exp.Value, typeArgs.MoveToImmutable(), context, out typeValue);
        }

        internal bool AnalyzeMemberExpAsType(QsMemberExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (!AnalyzeExpAsType(exp.Object, context, out var objTypeValue))
                return false;

            var typeArgs = ImmutableArray.CreateBuilder<QsTypeValue>(exp.MemberTypeArgs.Length);
            foreach (var typeArg in exp.MemberTypeArgs)
                typeArgs.Add(context.TypeValuesByTypeExp[typeArg]);

            if (!typeValueService.GetMemberTypeValue(objTypeValue, exp.MemberName, typeArgs.MoveToImmutable(), context.TypeValueServiceContext, out typeValue))
            {
                context.Errors.Add((exp, $"{objTypeValue}에 {exp.MemberName} 타입이 없습니다"));
                return false;
            }
            
            return true;
        }

        internal bool AnalyzeIdExp(QsIdentifierExp idExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            // 여기는 id가 변수/함수일때만 들어오는 부분
            if (idExp.TypeArgs.Length == 0 && context.VarTypeValues.TryGetValue(idExp.Value, out typeValue))
            {
                context.TypeValuesByExp.Add(idExp, typeValue);
                return true;
            }

            var typeArgs = ImmutableArray.CreateRange(idExp.TypeArgs, typeArg => context.TypeValuesByTypeExp[typeArg]);

            // TODO: this scope 변수, 함수 검색
            var candidates = new List<QsTypeValue>();
            if (idExp.TypeArgs.Length == 0 && context.GlobalVarTypeValues.TryGetValue(idExp.Value, out var globalVarTypeValue))
            {
                candidates.Add(globalVarTypeValue);                
            }

            if (context.GlobalFuncs.TryGetValue(idExp.Value, out var func))
            {
                var funcTypeValue = new QsFuncTypeValue(null, func.FuncId, typeArgs);
                candidates.Add(funcTypeValue);
            }

            foreach(var refMetadata in context.RefMetadatas)
            {
                if (idExp.TypeArgs.Length == 0 && refMetadata.GetGlobalVarTypeValue(idExp.Value, out var outVarTypeValue))
                    candidates.Add(outVarTypeValue);

                if (refMetadata.GetGlobalFuncTypeValue(idExp.Value, typeArgs, out var outFuncTypeValue))
                    candidates.Add(outFuncTypeValue);
            }

            if (candidates.Count == 1)
            {
                typeValue = candidates[0];
                context.TypeValuesByExp.Add(idExp, typeValue);
                return true;
            }
            else if (1 < candidates.Count)
            {
                typeValue = null;
                context.Errors.Add((idExp, "{idExp}가 모호합니다. 가능한 함수, 람다가 여러개 있습니다"));
                return false;
            }
            else
            {
                typeValue = null;
                context.Errors.Add((idExp, "가능한 함수, 람다가 없습니다"));
                return false;
            }
        }

        internal bool AnalyzeExpAsType(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            // NOTICE: 이 계열 함수는 TypeValuesByTypeExp에 추가하지 않는다

            return exp switch
            {
                QsIdentifierExp idExp => AnalyzeIdExpAsType(idExp, context, out typeValue),
                _ => throw new NotImplementedException()
            };
        }

        internal bool AnalyzeBoolLiteralExp(QsBoolLiteralExp boolExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            if (!analyzer.GetGlobalTypeValue("bool", context, out var boolTypeValue))
                Debug.Fail("Runtime에 bool이 없습니다");

            context.TypeValuesByExp.Add(boolExp, boolTypeValue);
            typeValue = boolTypeValue;
            return true;
        }

        internal bool AnalyzeIntLiteralExp(QsIntLiteralExp intExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            if (!analyzer.GetGlobalTypeValue("int", context, out var intTypeValue))
                Debug.Fail("Runtime에 int가 없습니다");

            context.TypeValuesByExp.Add(intExp, intTypeValue);
            typeValue = intTypeValue;
            return true;
        }

        internal bool AnalyzeStringExp(QsStringExp stringExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            if (!analyzer.GetGlobalTypeValue("string", context, out var stringTypeValue))
                Debug.Fail("Runtime에 string이 없습니다");

            context.TypeValuesByExp.Add(stringExp, stringTypeValue);
            typeValue = stringTypeValue;
            return true;
        }

        internal bool AnalyzeUnaryOpExp(QsUnaryOpExp unaryOpExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            // TODO: operator 함수 선택 방식 따로 만들기, 지금은 하드코딩
            if (!analyzer.GetGlobalTypeValue("bool", context, out var boolTypeValue) || 
                !analyzer.GetGlobalTypeValue("int", context, out var intTypeValue))
            {
                Debug.Fail("Runtime에 bool, int가 없습니다");
                return false;
            }

            if (!AnalyzeExp(unaryOpExp.Operand, context, out var operandTypeValue))            
                return false; // AnalyzeExp에서 에러가 생겼으므로 내부에서 에러를 추가했을 것이다. 여기서는 더 추가 하지 않는다

            switch (unaryOpExp.Kind)
            {
                case QsUnaryOpKind.LogicalNot:
                    {
                        if (!IsAssignable(boolTypeValue, operandTypeValue))
                        {
                            context.Errors.Add((unaryOpExp, $"{unaryOpExp.Operand}에 !를 적용할 수 없습니다. bool 타입이어야 합니다"));                            
                            return false;
                        }

                        context.TypeValuesByExp.Add(unaryOpExp, boolTypeValue);
                        typeValue = boolTypeValue;
                        return true;
                    }

                // TODO: operand가 lvalue인지 체크를 해줘야 한다..
                case QsUnaryOpKind.PostfixInc:
                case QsUnaryOpKind.PostfixDec:
                case QsUnaryOpKind.PrefixInc:
                case QsUnaryOpKind.PrefixDec:

                case QsUnaryOpKind.Minus:
                    {
                        if (!IsAssignable(intTypeValue, operandTypeValue))
                        {
                            context.Errors.Add((unaryOpExp, $"{unaryOpExp.Operand}에 -를 적용할 수 없습니다. int 타입이어야 합니다"));
                            return false;
                        }

                        context.TypeValuesByExp.Add(unaryOpExp, intTypeValue);
                        typeValue = intTypeValue;
                        return true;
                    }

                default:
                    context.Errors.Add((unaryOpExp, $"{operandTypeValue}를 지원하는 연산자가 없습니다"));
                    return false;
            }
        }

        internal bool AnalyzeBinaryOpExp(QsBinaryOpExp binaryOpExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (!analyzer.GetGlobalTypeValue("bool", context, out var boolTypeValue) ||
                !analyzer.GetGlobalTypeValue("int", context, out var intTypeValue) ||
                !analyzer.GetGlobalTypeValue("string", context, out var stringTypeValue))
            {
                Debug.Fail("Runtime에 bool, int가 없습니다");
                return false;
            }


            if (!AnalyzeExp(binaryOpExp.Operand0, context, out var operandTypeValue0))
                return false;

            if (!AnalyzeExp(binaryOpExp.Operand1, context, out var operandTypeValue1))
                return false;
            
            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
            {
                if (!IsAssignable(operandTypeValue0, operandTypeValue1))
                {
                    context.Errors.Add((binaryOpExp, $"{operandTypeValue1}를 {operandTypeValue0}에 대입할 수 없습니다"));
                    return false;
                }

                context.TypeValuesByExp.Add(binaryOpExp, operandTypeValue0);
                typeValue = operandTypeValue0;
                return true;
            }
            else if (binaryOpExp.Kind == QsBinaryOpKind.Equal || binaryOpExp.Kind == QsBinaryOpKind.NotEqual)
            {
                // TODO: 비교가능함은 어떻게 하나
                if (operandTypeValue0 != operandTypeValue1)
                {
                    context.Errors.Add((binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}을 비교할 수 없습니다"));
                    return false;
                }

                context.TypeValuesByExp.Add(binaryOpExp, boolTypeValue);
                typeValue = boolTypeValue;
                return true;
            }

            // TODO: 일단 하드코딩, Evaluator랑 지원하는 것들이 똑같아야 한다
            if (IsAssignable(boolTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(boolTypeValue, operandTypeValue1))
                {
                    context.Errors.Add((binaryOpExp, $"{operandTypeValue1}은 bool 형식이어야 합니다"));
                    return false;
                }

                switch (binaryOpExp.Kind)
                {
                    default:
                        context.Errors.Add((binaryOpExp, $"bool 형식에 적용할 수 있는 연산자가 아닙니다"));
                        return false;
                }
            }
            else if (IsAssignable(intTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(intTypeValue, operandTypeValue1))
                {
                    context.Errors.Add((binaryOpExp, $"{operandTypeValue1}은 int 형식이어야 합니다"));
                    return false;
                }

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Multiply:
                    case QsBinaryOpKind.Divide:
                    case QsBinaryOpKind.Modulo:
                    case QsBinaryOpKind.Add:
                    case QsBinaryOpKind.Subtract:
                        context.TypeValuesByExp.Add(binaryOpExp, intTypeValue);
                        typeValue = intTypeValue;
                        return true;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        context.TypeValuesByExp.Add(binaryOpExp, boolTypeValue);
                        typeValue = boolTypeValue;
                        return true;

                    default:
                        context.Errors.Add((binaryOpExp, $"int 형식에 적용할 수 있는 연산자가 아닙니다"));
                        return false;
                }
            }
            else if (IsAssignable(stringTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(stringTypeValue, operandTypeValue1))
                {
                    context.Errors.Add((binaryOpExp, $"{operandTypeValue1}은 string 형식이어야 합니다"));
                    return false;
                }

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Add:
                        context.TypeValuesByExp.Add(binaryOpExp, stringTypeValue);
                        typeValue = stringTypeValue;
                        return true;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        context.TypeValuesByExp.Add(binaryOpExp, boolTypeValue);
                        typeValue = boolTypeValue;
                        return true;

                    default:
                        context.Errors.Add((binaryOpExp, $"string 형식에 적용할 수 있는 연산자가 아닙니다"));
                        return false;
                }
            }

            context.Errors.Add((binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}를 지원하는 연산자가 없습니다"));
            return false;
        }        
        
        internal bool AnalyzeCallExp(QsCallExp callExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            // 여기서 분석해야 할 것은 
            // 1. 해당 Exp가 함수인지, 변수인지, 함수라면 FuncId를 넣어준다
            // 2. Callable 인자에 맞게 잘 들어갔는지 -> 완료
            // 3. 잘 들어갔다면 리턴타입 -> 완료

            typeValue = null;

            var builder = ImmutableArray.CreateBuilder<QsTypeValue>(callExp.Args.Length);
            foreach (var arg in callExp.Args)
            {
                if (!AnalyzeExp(arg, context, out var argTypeValue))
                    return false; 

                builder.Add(argTypeValue);
            }
            var argTypeValues = builder.MoveToImmutable();

            // TODO: 함수 오버로딩이 들어가면 AnalyzeExp 함수만으로 안될 것이다
            if (!AnalyzeExp(callExp.Callable, context, out var callableTypeValue))
                return false;

            var funcTypeValue = callableTypeValue as QsFuncTypeValue;
            if (funcTypeValue == null)
            {
                context.Errors.Add((callExp.Callable, $"{callExp.Callable}은 호출 가능한 타입이 아닙니다"));
                return false;
            }

            if (!analyzer.GetReturnTypeValue(funcTypeValue, context, out var retTypeValue))
                return false;

            if (!analyzer.GetParamTypeValues(funcTypeValue, context, out var paramTypeValues))
                return false;

            if (paramTypeValues.Value.Length != argTypeValues.Length)
            {
                context.Errors.Add((callExp, $"함수는 인자를 {paramTypeValues.Value.Length}개 받는데, 호출 인자는 {argTypeValues.Length} 개입니다"));
                return false;
            }

            for(int i = 0; i < paramTypeValues.Value.Length; i++)
            {
                if (!IsAssignable(paramTypeValues.Value[i], argTypeValues[i]))
                {
                    context.Errors.Add((callExp.Args[i], $"함수의 {i + 1}번 째 매개변수 타입은 {paramTypeValues.Value[i]} 인데, 호출 인자 타입은 {argTypeValues[i]} 입니다"));
                }
            }

            typeValue = retTypeValue;
            return true;
        }

        internal bool AnalyzeLambdaExp(QsLambdaExp lambdaExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            throw new NotImplementedException();
        }

        internal bool AnalyzeMemberCallExp(QsMemberCallExp memberCallExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            throw new NotImplementedException();
        }

        internal bool AnalyzeMemberExp(QsMemberExp memberExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            // TODO: 함수 지원, Lambda로 묶어주면 된다
            // TODO: AccessModifier

            // 변수라면 
            if (memberExp.MemberTypeArgs.Length == 0 && AnalyzeExp(memberExp.Object, context, out var objTypeValue))
            {
                if (!typeValueService.GetMemberVarTypeValue(false, objTypeValue, memberExp.MemberName, context.TypeValueServiceContext, out typeValue))
                {
                    context.Errors.Add((memberExp, $"{objTypeValue}에 {memberExp.MemberName}가 없습니다"));
                    return false;
                }

                context.TypeValuesByExp.Add(memberExp, typeValue);
                return true;
            }

            // 타입이라면, 정적 변수에 접근 할 수 있다
            // E.First,  
            else if (AnalyzeExpAsType(memberExp.Object, context, out var staticObjTypeValue))
            {
                if (!typeValueService.GetMemberVarTypeValue(true, staticObjTypeValue, memberExp.MemberName, context.TypeValueServiceContext, out typeValue))
                {
                    context.Errors.Add((memberExp, $"{staticObjTypeValue}에 정적 변수 {memberExp.MemberName}가 없습니다"));
                    return false;
                }

                context.TypeValuesByExp.Add(memberExp, typeValue);
                return true;
            }

            context.Errors.Add((memberExp, $"{memberExp.Object}에 {memberExp.MemberName} 이름의 멤버를 찾을 수 없습니다"));
            typeValue = null;
            return false;
        }

        internal bool AnalyzeListExp(QsListExp listExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;
            QsTypeValue? curElemTypeValue = null;

            foreach (var elem in listExp.Elems)
            {
                if (!AnalyzeExp(elem, context, out var elemTypeValue))
                    return false;

                if (curElemTypeValue == null)
                {
                    curElemTypeValue = elemTypeValue;
                    continue;
                }

                if (curElemTypeValue != elemTypeValue)
                {
                    // TODO: 둘의 공통 조상을 찾아야 하는지 결정을 못했다..
                    context.Errors.Add((listExp, $"원소 {elem}의 타입이 {curElemTypeValue} 가 아닙니다"));
                    return false;
                }
            }

            if (curElemTypeValue == null)
            {
                context.Errors.Add((listExp, $"리스트의 타입을 결정하지 못했습니다"));
                return false;
            }

            if (!analyzer.GetGlobalTypeValue("List", ImmutableArray.Create(curElemTypeValue), context, out typeValue))
            {
                Debug.Fail("Runtime에 리스트가 없습니다");
                return false;
            }

            context.TypeValuesByExp.Add(listExp, typeValue);
            return true;
        }

        public bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            return exp switch
            {
                QsIdentifierExp idExp => AnalyzeIdExp(idExp, context, out typeValue),
                QsBoolLiteralExp boolExp => AnalyzeBoolLiteralExp(boolExp, context, out typeValue),
                QsIntLiteralExp intExp => AnalyzeIntLiteralExp(intExp, context, out typeValue),
                QsStringExp stringExp => AnalyzeStringExp(stringExp, context, out typeValue),
                QsUnaryOpExp unaryOpExp => AnalyzeUnaryOpExp(unaryOpExp, context, out typeValue),
                QsBinaryOpExp binaryOpExp => AnalyzeBinaryOpExp(binaryOpExp, context, out typeValue),
                QsCallExp callExp => AnalyzeCallExp(callExp, context, out typeValue),
                QsLambdaExp lambdaExp => AnalyzeLambdaExp(lambdaExp, context, out typeValue),
                QsMemberCallExp memberCallExp => AnalyzeMemberCallExp(memberCallExp, context, out typeValue),
                QsMemberExp memberExp => AnalyzeMemberExp(memberExp, context, out typeValue),
                QsListExp listExp => AnalyzeListExp(listExp, context, out typeValue),

                _ => throw new NotImplementedException()
            };
        }
    }
}
