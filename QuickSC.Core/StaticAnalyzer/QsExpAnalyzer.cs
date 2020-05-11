using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    class QsExpAnalyzer
    {
        QsTypeValueFactory typeValueFactory;
        QsTypeValue boolTypeValue;
        QsTypeValue intTypeValue;
        QsTypeValue stringTypeValue;        

        public QsExpAnalyzer(QsTypeValueFactory typeValueFactory)
        {
            this.typeValueFactory = typeValueFactory;
            boolTypeValue = typeValueFactory.GetTypeValue("bool") ?? throw new InvalidOperationException();
            intTypeValue = typeValueFactory.GetTypeValue("int") ?? throw new InvalidOperationException();
            stringTypeValue = typeValueFactory.GetTypeValue("string") ?? throw new InvalidOperationException();
        }

        QsTypeValue? AnalyzeIdExp(QsIdentifierExp idExp, QsAnalyzerContext context) 
        {
            // 여기는 id가 변수일때만 들어오는 부분
            QsTypeValue? typeValue;
            if (context.GetVarTypeValue(idExp.Value, out typeValue))
            {
                context.AddExpTypeValue(idExp, typeValue);
                return typeValue;
            }

            if (context.GetGlobalVarTypeValue(idExp.Value, out typeValue))
            {
                context.AddExpTypeValue(idExp, typeValue);
                return typeValue;
            }

            return null;            
        }

        QsTypeValue? AnalyzeBoolLiteralExp(QsBoolLiteralExp boolExp, QsAnalyzerContext context) 
        {            
            context.AddExpTypeValue(boolExp, boolTypeValue);
            return boolTypeValue;
        }

        internal QsTypeValue? AnalyzeIntLiteralExp(QsIntLiteralExp intExp, QsAnalyzerContext context)
        {
            context.AddExpTypeValue(intExp, intTypeValue);
            return intTypeValue;
        }

        QsTypeValue? AnalyzeStringExp(QsStringExp stringExp, QsAnalyzerContext context)
        {
            context.AddExpTypeValue(stringExp, stringTypeValue);
            return stringTypeValue;
        }

        QsTypeValue? AnalyzeUnaryOpExp(QsUnaryOpExp unaryOpExp, QsAnalyzerContext context) 
        {
            // TODO: operator 함수 선택 방식 따로 만들기, 지금은 하드코딩
            var operandTypeValue = AnalyzeExp(unaryOpExp.OperandExp, context);
            if (operandTypeValue == null) // AnalyzeExp에서 에러가 생겼으므로 여기서 에러를 추가하지 않는다
                return null;

            switch(unaryOpExp.Kind)
            {   
                case QsUnaryOpKind.LogicalNot:
                    {
                        if (!IsAssignable(boolTypeValue, operandTypeValue))
                        {
                            context.AddError(unaryOpExp.OperandExp, $"{unaryOpExp.OperandExp}에 !를 적용할 수 없습니다. bool 타입이어야 합니다");
                            return null;
                        }

                        context.AddExpTypeValue(unaryOpExp, boolTypeValue);
                        return boolTypeValue;
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
                            context.AddError(unaryOpExp.OperandExp, $"{unaryOpExp.OperandExp}에 -를 적용할 수 없습니다. int 타입이어야 합니다");
                            return null;
                        }

                        context.AddExpTypeValue(unaryOpExp, intTypeValue);
                        return intTypeValue;
                    }

                default:
                    context.AddError(unaryOpExp, $"{operandTypeValue}를 지원하는 연산자가 없습니다");
                    return null;
            }
        }

        QsTypeValue? AnalyzeBinaryOpExp(QsBinaryOpExp binaryOpExp, QsAnalyzerContext context) 
        {
            var operandTypeValue0 = AnalyzeExp(binaryOpExp.Operand0, context);
            if (operandTypeValue0 == null) return null;

            var operandTypeValue1 = AnalyzeExp(binaryOpExp.Operand1, context);
            if (operandTypeValue1 == null) return null;

            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
            {
                if (!IsAssignable(operandTypeValue0, operandTypeValue1))
                {
                    context.AddError(binaryOpExp, $"{operandTypeValue1}를 {operandTypeValue0}에 대입할 수 없습니다");
                    return null;
                }

                context.AddExpTypeValue(binaryOpExp, operandTypeValue0);
                return operandTypeValue0;
            }
            else if (binaryOpExp.Kind == QsBinaryOpKind.Equal || binaryOpExp.Kind == QsBinaryOpKind.NotEqual)
            {
                // TODO: 비교가능함은 어떻게 하나
                if (operandTypeValue0 != operandTypeValue1)
                {
                    context.AddError(binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}을 비교할 수 없습니다");
                    return null;
                }

                context.AddExpTypeValue(binaryOpExp, boolTypeValue);
                return boolTypeValue;
            }

            // TODO: 일단 하드코딩, Evaluator랑 지원하는 것들이 똑같아야 한다
            if (IsAssignable(boolTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(boolTypeValue, operandTypeValue1))
                {
                    context.AddError(binaryOpExp, $"{operandTypeValue1}은 bool 형식이어야 합니다");
                    return null;
                }

                switch (binaryOpExp.Kind)
                {
                    default:
                        context.AddError(binaryOpExp, $"bool 형식에 적용할 수 있는 연산자가 아닙니다");
                        return null;
                }
            }
            else if (IsAssignable(intTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(intTypeValue, operandTypeValue1))
                {
                    context.AddError(binaryOpExp, $"{operandTypeValue1}은 int 형식이어야 합니다");
                    return null;
                }

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Multiply:
                    case QsBinaryOpKind.Divide:
                    case QsBinaryOpKind.Modulo:
                    case QsBinaryOpKind.Add:
                    case QsBinaryOpKind.Subtract:
                        context.AddExpTypeValue(binaryOpExp, intTypeValue);
                        return intTypeValue;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        context.AddExpTypeValue(binaryOpExp, boolTypeValue);
                        return boolTypeValue;

                    default:
                        context.AddError(binaryOpExp, $"int 형식에 적용할 수 있는 연산자가 아닙니다");
                        return null;
                }
            }            
            else if (IsAssignable(stringTypeValue, operandTypeValue0))
            {
                if (!IsAssignable(stringTypeValue, operandTypeValue1))
                {
                    context.AddError(binaryOpExp, $"{operandTypeValue1}은 string 형식이어야 합니다");
                    return null;
                }

                switch (binaryOpExp.Kind)
                {
                    case QsBinaryOpKind.Add:
                        context.AddExpTypeValue(binaryOpExp, stringTypeValue);
                        return stringTypeValue;

                    case QsBinaryOpKind.LessThan:
                    case QsBinaryOpKind.GreaterThan:
                    case QsBinaryOpKind.LessThanOrEqual:
                    case QsBinaryOpKind.GreaterThanOrEqual:
                        context.AddExpTypeValue(binaryOpExp, boolTypeValue);
                        return boolTypeValue;

                    default:
                        context.AddError(binaryOpExp, $"string 형식에 적용할 수 있는 연산자가 아닙니다");
                        return null;
                }
            }

            context.AddError(binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}를 지원하는 연산자가 없습니다");
            return null;
        }

        QsTypeValue? AnalyzeCallExp(QsCallExp callExp, QsAnalyzerContext context) { throw new NotImplementedException(); }

        QsTypeValue? AnalyzeLambdaExp(QsLambdaExp lambdaExp, QsAnalyzerContext context) 
        { 
            throw new NotImplementedException(); 
        }

        QsTypeValue? AnalyzeMemberCallExp(QsMemberCallExp memberCallExp, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsTypeValue? AnalyzeMemberExp(QsMemberExp memberExp, QsAnalyzerContext context) { throw new NotImplementedException(); }

        QsTypeValue? AnalyzeListExp(QsListExp listExp, QsAnalyzerContext context) 
        {
            QsTypeValue? curElemTypeValue = null;

            foreach(var elem in listExp.Elems)
            {
                var elemTypeValue = AnalyzeExp(elem, context);
                if (elemTypeValue == null) return null;

                if (curElemTypeValue == null)
                {
                    curElemTypeValue = elemTypeValue;
                    continue;
                }

                if (curElemTypeValue != elemTypeValue)
                {
                    // TODO: 둘의 공통 조상을 찾아야 하는지 결정을 못했다..
                    context.AddError(listExp, $"원소 {elem}의 타입이 {curElemTypeValue} 가 아닙니다");
                    return null;
                }
            }

            if (curElemTypeValue == null)
            {                
                context.AddError(listExp, $"리스트의 타입을 결정하지 못했습니다");
                return null;
            }

            return typeValueFactory.GetTypeValue("List", curElemTypeValue);
        }

        public QsTypeValue? AnalyzeExp(QsExp exp, QsAnalyzerContext context)
        {
            return exp switch
            {
                QsIdentifierExp idExp => AnalyzeIdExp(idExp, context),
                QsBoolLiteralExp boolExp => AnalyzeBoolLiteralExp(boolExp, context),
                QsIntLiteralExp intExp => AnalyzeIntLiteralExp(intExp, context),
                QsStringExp stringExp => AnalyzeStringExp(stringExp, context),
                QsUnaryOpExp unaryOpExp => AnalyzeUnaryOpExp(unaryOpExp, context),
                QsBinaryOpExp binaryOpExp => AnalyzeBinaryOpExp(binaryOpExp, context),
                QsCallExp callExp => AnalyzeCallExp(callExp, context),
                QsLambdaExp lambdaExp => AnalyzeLambdaExp(lambdaExp, context),
                QsMemberCallExp memberCallExp => AnalyzeMemberCallExp(memberCallExp, context),
                QsMemberExp memberExp => AnalyzeMemberExp(memberExp, context),
                QsListExp listExp => AnalyzeListExp(listExp, context),

                _ => throw new NotImplementedException()
            };
        }
    }
}
