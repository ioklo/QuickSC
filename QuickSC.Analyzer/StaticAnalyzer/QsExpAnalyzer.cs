using QuickSC;
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

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzerIdentifierInfo
    {
        public class Var : QsAnalyzerIdentifierInfo
        {
            public QsStorage Storage { get; }
            public QsTypeValue TypeValue { get; }

            public Var(QsStorage storage, QsTypeValue typeValue)
            {
                Storage = storage;
                TypeValue = typeValue;
            }
        }

        public class Func : QsAnalyzerIdentifierInfo
        {
            public QsFuncValue FuncValue { get; }
            public Func(QsFuncValue funcValue)
            {
                FuncValue = funcValue;
            }
        }

        public class Type : QsAnalyzerIdentifierInfo
        {
            public QsTypeValue TypeValue { get; }
            public Type(QsTypeValue typeValue)
            {
                TypeValue = typeValue;
            }
        }

        public static Var MakeVar(QsStorage storage, QsTypeValue typeValue) => new Var(storage, typeValue);

        public static Func MakeFunc(QsFuncValue funcValue) => new Func(funcValue);

        public static Type MakeType(QsTypeValue typeValue) => new Type(typeValue);
    }

    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    partial class QsExpAnalyzer
    {
        QsAnalyzer analyzer;        

        public QsExpAnalyzer(QsAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        // x
        internal bool AnalyzeIdExp(QsIdentifierExp idExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            var typeArgs = QsAnalyzerMisc.GetTypeValues(idExp.TypeArgs, context);

            if (!context.GetIdentifierInfo(idExp.Value, typeArgs, out var idInfo))
                return false;

            if (idInfo is QsAnalyzerIdentifierInfo.Var varIdInfo)
            {
                outTypeValue = varIdInfo.TypeValue;
                context.AddNodeInfo(idExp, new QsIdentifierExpInfo(varIdInfo.Storage));
                return true;
            }

            // TODO: Func
            return false;
        }

        internal bool AnalyzeBoolLiteralExp(QsBoolLiteralExp boolExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {   
            typeValue = analyzer.GetBoolTypeValue();
            return true;
        }

        internal bool AnalyzeIntLiteralExp(QsIntLiteralExp intExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = analyzer.GetIntTypeValue();
            return true;
        }

        internal bool AnalyzeStringExp(QsStringExp stringExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            foreach(var elem in stringExp.Elements)
                analyzer.AnalyzeStringExpElement(elem, context);

            typeValue = analyzer.GetStringTypeValue();
            return true;
        }

        internal bool AnalyzeUnaryOpExp(QsUnaryOpExp unaryOpExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            var boolTypeValue = analyzer.GetBoolTypeValue();
            var intTypeValue = analyzer.GetIntTypeValue();
            
            if (!AnalyzeExp(unaryOpExp.Operand, context, out var operandTypeValue))            
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

                // TODO: operand가 lvalue인지 체크를 해줘야 한다..
                case QsUnaryOpKind.PostfixInc:
                case QsUnaryOpKind.PostfixDec:
                case QsUnaryOpKind.PrefixInc:
                case QsUnaryOpKind.PrefixDec:

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

        internal bool AnalyzeBinaryOpExp(QsBinaryOpExp binaryOpExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            var boolTypeValue = analyzer.GetBoolTypeValue();
            var intTypeValue = analyzer.GetIntTypeValue();
            var stringTypeValue = analyzer.GetStringTypeValue();

            if (!AnalyzeExp(binaryOpExp.Operand0, context, out var operandTypeValue0))
                return false;

            if (!AnalyzeExp(binaryOpExp.Operand1, context, out var operandTypeValue1))
                return false;
            
            if (binaryOpExp.Kind == QsBinaryOpKind.Assign)
            {
                if (!analyzer.IsAssignable(operandTypeValue0, operandTypeValue1, context))
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue1}를 {operandTypeValue0}에 대입할 수 없습니다");
                    return false;
                }

                typeValue = operandTypeValue0;
                return true;
            }
            else if (binaryOpExp.Kind == QsBinaryOpKind.Equal || binaryOpExp.Kind == QsBinaryOpKind.NotEqual)
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
            QsIdentifierExp exp, ImmutableArray<QsTypeValue> args, QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue_Func TypeValue)? outValue)
        {
            // 1. this 검색

            // 2. global 검색
            var funcId = new QsMetaItemId(new QsMetaItemIdElem(exp.Value, exp.TypeArgs.Length));
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

                var funcValue = new QsFuncValue(null, globalFunc.FuncId, typeArgs);
                var funcTypeValue = context.TypeValueService.GetTypeValue(funcValue);

                if (!CheckParamTypes(exp, funcTypeValue.Params, args, context))
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
            QsExp exp, ImmutableArray<QsTypeValue> args, QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue_Func TypeValue)? outValue)
        {
            if (!AnalyzeExp(exp, context, out var typeValue))
            {
                outValue = null;
                return false;
            }

            var funcTypeValue = typeValue as QsTypeValue_Func;
            if (funcTypeValue == null)
            {
                context.ErrorCollector.Add(exp, $"호출 가능한 타입이 아닙니다");
                outValue = null;
                return false;
            }

            if (!CheckParamTypes(exp, funcTypeValue.Params, args, context))
            {
                outValue = null;
                return false;
            }

            outValue = (null, funcTypeValue);
            return true;
        }

        bool CheckParamTypes(object objForErrorMsg, ImmutableArray<QsTypeValue> parameters, ImmutableArray<QsTypeValue> args, QsAnalyzerContext context)
        {
            if (parameters.Length != args.Length)
            {
                context.ErrorCollector.Add(objForErrorMsg, $"함수는 인자를 {parameters.Length}개 받는데, 호출 인자는 {args.Length} 개입니다");
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!analyzer.IsAssignable(parameters[i], args[i], context))
                {
                    context.ErrorCollector.Add(objForErrorMsg, $"함수의 {i + 1}번 째 매개변수 타입은 {parameters[i]} 인데, 호출 인자 타입은 {args[i]} 입니다");
                    return false;
                }
            }

            return true;
        }

        // FuncValue도 같이 리턴한다
        // CallExp(F, [1]); // F(1)
        //   -> AnalyzeCallableExp(F, [Int])
        //        -> FuncValue(
        bool AnalyzeCallableExp(
            QsExp exp, 
            ImmutableArray<QsTypeValue> args, QsAnalyzerContext context, 
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsTypeValue_Func TypeValue)? outValue)
        {
            if (exp is QsIdentifierExp idExp)
                return AnalyzeCallableIdentifierExp(idExp, args, context, out outValue);
            else
                return AnalyzeCallableElseExp(exp, args, context, out outValue);
        }
        
        internal bool AnalyzeCallExp(QsCallExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue) 
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
        
        internal bool AnalyzeLambdaExp(QsLambdaExp lambdaExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
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

        bool AnalyzeIndexerExp(QsIndexerExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            if (!AnalyzeExp(exp.Object, context, out var objTypeValue))
                return false;

            if (!AnalyzeExp(exp.Index, context, out var indexTypeValue))
                return false;

            // objTypeValue에 indexTypeValue를 인자로 갖고 있는 indexer가 있는지
            if (!context.TypeValueService.GetMemberFuncValue(objTypeValue, QsName.Special(QsSpecialName.Indexer), ImmutableArray<QsTypeValue>.Empty, out var funcValue))
            {
                context.ErrorCollector.Add(exp, "객체에 indexer함수가 없습니다");
                return false;
            }
            
            if (QsAnalyzerMisc.IsFuncStatic(funcValue.FuncId, context))
            {
                Debug.Fail("객체에 indexer가 있는데 Static입니다");
                return false;
            }

            var funcTypeValue = context.TypeValueService.GetTypeValue(funcValue);

            if (!CheckParamTypes(exp, funcTypeValue.Params, ImmutableArray.Create(indexTypeValue), context))
                return false;

            context.AddNodeInfo(exp, new QsIndexerExpInfo(funcValue, objTypeValue, indexTypeValue));

            outTypeValue = funcTypeValue.Return;
            return true;
        }

        bool AnalyzeExps(ImmutableArray<QsExp> exps, QsAnalyzerContext context, out ImmutableArray<QsTypeValue> outTypeValues)
        {
            var builder = ImmutableArray.CreateBuilder<QsTypeValue>(exps.Length);
            foreach (var exp in exps)
            {
                if (!AnalyzeExp(exp, context, out var typeValue))
                {
                    outTypeValues = ImmutableArray<QsTypeValue>.Empty;
                    return false;
                }

                builder.Add(typeValue);
            }

            outTypeValues = builder.MoveToImmutable();
            return true;
        }

        internal bool AnalyzeMemberCallExp(QsMemberCallExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;

            var result = new MemberCallExpAnalyzer(this, exp, context).Analyze();
            if (result == null) return false;

            if (!CheckParamTypes(exp, result.Value.TypeValue.Params, result.Value.ArgTypeValues, context))
                return false;

            context.AddNodeInfo(exp, result.Value.NodeInfo);
            outTypeValue = result.Value.TypeValue.Return;
            return true;
        }

        internal bool AnalyzeMemberExp(QsMemberExp memberExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue) 
        {
            var analyzer = new MemberExpAnalyzer(this, memberExp, context);
            var result = analyzer.Analyze();

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

            var typeInfos = context.MetadataService.GetTypeInfos(new QsMetaItemId(new QsMetaItemIdElem("List", 1))).ToImmutableArray();            

            if (typeInfos.Length != 1)
            {
                Debug.Fail("Runtime에 적합한 리스트가 없습니다");
                return false;
            }

            typeValue = new QsTypeValue_Normal(null, typeInfos[0].TypeId, curElemTypeValue);
            context.AddNodeInfo(listExp, new QsListExpInfo(curElemTypeValue));
            return true;
        }

        public bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            switch(exp)
            {
                case QsIdentifierExp idExp: return AnalyzeIdExp(idExp, context, out typeValue);
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
