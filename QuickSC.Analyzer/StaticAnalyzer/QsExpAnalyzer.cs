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
    // 어떤 Exp에서 타입 정보 등을 알아냅니다
    class QsExpAnalyzer
    {
        QsAnalyzer analyzer;
        QsCapturer capturer;
        QsTypeValueService typeValueService;        

        public QsExpAnalyzer(QsAnalyzer analyzer, QsCapturer capturer, QsTypeValueService typeValueService)
        {
            this.analyzer = analyzer;
            this.capturer = capturer;
            this.typeValueService = typeValueService;
        }

        internal bool AnalyzeIdExp(QsIdentifierExp idExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            if (AnalyzeIdExpWithStatic(idExp, context, out var outValue))
            {
                if (!outValue.Value.bStatic)
                {
                    context.TypeValuesByExp.Add(idExp, outValue.Value.TypeValue);
                    outTypeValue = outValue.Value.TypeValue;
                    return true;
                }
            }

            outTypeValue = null;
            return false;
        }

        

        internal bool AnalyzeIdExpWithStatic(QsIdentifierExp idExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out (bool bStatic, QsTypeValue TypeValue)? outValue)
        {
            QsTypeValue? typeValue;

            // 변수에서 검색
            if (context.CurFunc.GetVarInfo(idExp.Value, out var localVarInfo))
            {
                if (idExp.TypeArgs.Length != 0)
                {
                    context.Errors.Add((idExp, $"{idExp.Value} 변수는 타입인자를 가질 수 없습니다"));
                    outValue = null;
                    return false;
                }

                // Local
                context.EvalExpsByExp.Add(idExp, new QsLocalVarStorage(localVarInfo.Index));
                context.TypeValuesByExp.Add(idExp, localVarInfo.TypeValue);
                outValue = (false, localVarInfo.TypeValue);
                return true;
            }

            var typeArgs = ImmutableArray.CreateRange(idExp.TypeArgs, typeArg => context.TypeValuesByTypeExp[typeArg]);

            // TODO: this scope 변수, 함수 검색            

            // 전역 변수/함수, 레퍼런스에서 검색
            var candidates = new List<(QsTypeValue TypeValue, QsStorage Storage)>();
            if (idExp.TypeArgs.Length == 0 && analyzer.GetGlobalVar(idExp.Value, context, out var globalVar))
            {
                // GlobalVar
                candidates.Add((globalVar.TypeValue, new QsGlobalVarStorage(globalVar.VarId)));
            }

            // TODO: GlobalFunc Lambda 지원 Test\Lambda\05_FuncAsLambda.qs
            //if (analyzer.GetGlobalFunc(idExp.Value, context, out var func))
            //{
            //    // GlobalFunc
            //    var funcTypeValue = typeValueService.MakeFuncTypeValue(null, func, typeArgs, context.TypeValueServiceContext);
            //    candidates.Add((funcTypeValue, QsGlobalStorage.Instance, QsStorageKind.Func));
            //}

            if (candidates.Count == 1)
            {   
                context.TypeValuesByExp.Add(idExp, candidates[0].TypeValue);
                context.EvalExpsByExp.Add(idExp, candidates[0].Storage);
                outValue = (false, candidates[0].TypeValue);
                return true;
            }
            else if (1 < candidates.Count)
            {
                outValue = null;
                context.Errors.Add((idExp, $"{idExp}가 모호합니다. 가능한 함수, 람다가 여러개 있습니다"));
                return false;
            }

            // 전역/레퍼런스 타입에서 검색, GlobalType, RefType
            if (analyzer.GetGlobalTypeValue(idExp.Value, typeArgs, context, out typeValue))
            {
                // 여기는 MemberExp로부터만 오기 때문에 MemberExp에 추가해본다
                outValue = (true, typeValue);
                return true;
            }

            outValue = null;
            context.Errors.Add((idExp, "가능한 함수, 람다가 없습니다"));
            return false;
        }

        internal bool AnalyzeExpWithStatic(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out (bool bStatic, QsTypeValue TypeValue)? outValue)
        {
            if (exp is QsIdentifierExp idExp)
                return AnalyzeIdExpWithStatic(idExp, context, out outValue);

            if (AnalyzeExp(exp, context, out var typeValue))
            {
                outValue = (false, typeValue);
                return true;
            }
            else
            {
                outValue = null;
                return false;
            }
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
                        if (!analyzer.IsAssignable(boolTypeValue, operandTypeValue, context))
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
                        if (!analyzer.IsAssignable(intTypeValue, operandTypeValue, context))
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
                if (!analyzer.IsAssignable(operandTypeValue0, operandTypeValue1, context))
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
            if (analyzer.IsAssignable(boolTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(boolTypeValue, operandTypeValue1, context))
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
            else if (analyzer.IsAssignable(intTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(intTypeValue, operandTypeValue1, context))
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
            else if (analyzer.IsAssignable(stringTypeValue, operandTypeValue0, context))
            {
                if (!analyzer.IsAssignable(stringTypeValue, operandTypeValue1, context))
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
        
        internal bool AnalyzeCallExp(QsCallExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            // 여기서 분석해야 할 것은 
            // 1. 해당 Exp가 함수인지, 변수인지, 함수라면 FuncId를 넣어준다
            // 2. Callable 인자에 맞게 잘 들어갔는지 -> 완료
            // 3. 잘 들어갔다면 리턴타입 -> 완료

            typeValue = null;

            var builder = ImmutableArray.CreateBuilder<QsTypeValue>(exp.Args.Length);
            foreach (var arg in exp.Args)
            {
                if (!AnalyzeExp(arg, context, out var argTypeValue))
                    return false; 

                builder.Add(argTypeValue);
            }
            var argTypeValues = builder.MoveToImmutable();

            // TODO: 함수 오버로딩이 들어가면 AnalyzeExp 함수만으로 안될 것이다
            if (!AnalyzeExp(exp.Callable, context, out var callableTypeValue))
                return false;

            var funcTypeValue = callableTypeValue as QsFuncTypeValue;
            if (funcTypeValue == null)
            {
                context.Errors.Add((exp.Callable, $"{exp.Callable}은 호출 가능한 타입이 아닙니다"));
                return false;
            }
            
            if (funcTypeValue.ParamTypeValues.Length != argTypeValues.Length)
            {
                context.Errors.Add((exp, $"함수는 인자를 {funcTypeValue.ParamTypeValues.Length}개 받는데, 호출 인자는 {argTypeValues.Length} 개입니다"));
                return false;
            }

            for(int i = 0; i < funcTypeValue.ParamTypeValues.Length; i++)
            {
                if (!analyzer.IsAssignable(funcTypeValue.ParamTypeValues[i], argTypeValues[i], context))
                {
                    context.Errors.Add((exp.Args[i], $"함수의 {i + 1}번 째 매개변수 타입은 {funcTypeValue.ParamTypeValues[i]} 인데, 호출 인자 타입은 {argTypeValues[i]} 입니다"));
                }
            }

            typeValue = funcTypeValue.RetTypeValue;
            context.TypeValuesByExp.Add(exp, typeValue);
            return true;
        }

        // TODO: typeSkeleton 단계에서 LambdaExp에 타입 부여하기, AnalyzePhase에서 타입 생성하기
        internal bool AnalyzeLambdaExp(QsLambdaExp lambdaExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            // capture에 필요한 정보를 가져옵니다
            if (!capturer.Capture(lambdaExp.Body, out var captureResult))
            {
                context.Errors.Add((lambdaExp, "변수 캡쳐에 실패했습니다"));
                outTypeValue = null;
                return false;
            }            

            // 람다 함수 컨텍스트를 만든다
            var lambdaFuncId = new QsFuncId(null, context.CurFunc.FuncId.Elems.Add(
                    new QsNameElem(QsName.AnonymousLambda(context.CurFunc.LambdaCount.ToString()), 0)));
            context.CurFunc.LambdaCount++;

            // 캡쳐된 variable은 새 VarId를 가져야 한다
            var func = new QsAnalyzerFuncContext(lambdaFuncId, null, false);

            var (prevFunc, bPrevGlobalScope) = (context.CurFunc, context.bGlobalScope);
            context.bGlobalScope = false;
            context.CurFunc = func;

            // 필요한 변수들을 찾는다
            var elemsBuilder = ImmutableArray.CreateBuilder<QsLambdaEvalExp.Elem>(captureResult.NeedCaptures.Length);
            foreach (var needCapture in captureResult.NeedCaptures)
            {
                if (context.CurFunc.GetVarInfo(needCapture.VarName, out var localVarInfo))
                {
                    elemsBuilder.Add(new QsLambdaEvalExp.Elem(needCapture.Kind, new QsLocalIdExp(localVarInfo.Index)));
                    context.CurFunc.AddVarInfo(needCapture.VarName, localVarInfo.TypeValue);
                }
                else if (analyzer.GetGlobalVar(needCapture.VarName, context, out var globalVar))
                {
                    elemsBuilder.Add(new QsLambdaEvalExp.Elem(needCapture.Kind, new QsGlobalIdExp(globalVar.VarId)));
                    context.CurFunc.AddVarInfo(needCapture.VarName, globalVar.TypeValue);
                }
                else
                {
                    context.Errors.Add((lambdaExp, "캡쳐실패"));
                    outTypeValue = null;
                    return false;
                }
            }
            
            var paramTypeValuesBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(lambdaExp.Params.Length);
            foreach (var param in lambdaExp.Params)
            {
                if (param.Type == null)
                {
                    context.Errors.Add((param, "람다 인자 타입추론은 아직 지원하지 않습니다"));
                    outTypeValue = null;
                    return false;
                }

                var paramTypeValue = context.TypeValuesByTypeExp[param.Type];

                paramTypeValuesBuilder.Add(paramTypeValue);
                context.CurFunc.AddVarInfo(param.Name, paramTypeValue);
            }

            analyzer.AnalyzeStmt(lambdaExp.Body, context);
            
            context.bGlobalScope = bPrevGlobalScope;            
            context.CurFunc = prevFunc;

            outTypeValue = new QsFuncTypeValue(
                func.RetTypeValue ?? QsVoidTypeValue.Instance, 
                paramTypeValuesBuilder.MoveToImmutable());

            context.EvalExpsByExp[lambdaExp] = new QsLambdaEvalExp(elemsBuilder.MoveToImmutable(), lambdaExp);
            return true;
        }

        internal bool AnalyzeMemberCallExp(QsMemberCallExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            typeValue = null;

            var builder = ImmutableArray.CreateBuilder<QsTypeValue>(exp.Args.Length);
            foreach (var arg in exp.Args)
            {
                if (!AnalyzeExp(arg, context, out var argTypeValue))
                    return false;

                builder.Add(argTypeValue);
            }
            var argTypes = builder.MoveToImmutable();

            if (!AnalyzeExpWithStatic(exp.Object, context, out var outValue))
                return false;

            if (!analyzer.GetMemberFuncTypeValue(outValue.Value.bStatic, outValue.Value.TypeValue, new QsName(exp.MemberFuncName), context, out var funcType))
            {
                context.Errors.Add((exp, $"{exp.Object}에 {exp.MemberFuncName} 함수가 없습니다"));
                return false;
            }

            if (funcType.ParamTypeValues.Length != argTypes.Length)
            {
                context.Errors.Add((exp, $"함수는 인자를 {funcType.ParamTypeValues.Length}개 받는데, 호출 인자는 {argTypes.Length} 개입니다"));
                return false;
            }

            for (int i = 0; i < funcType.ParamTypeValues.Length; i++)
            {
                if (!analyzer.IsAssignable(funcType.ParamTypeValues[i], argTypes[i], context))
                {
                    context.Errors.Add((exp.Args[i], $"함수의 {i + 1}번 째 매개변수 타입은 {funcType.ParamTypeValues[i]} 인데, 호출 인자 타입은 {argTypes[i]} 입니다"));
                }
            }

            typeValue = funcType.RetTypeValue;
            context.TypeValuesByExp.Add(exp, typeValue);
            return true;
        }

        internal bool AnalyzeMemberExp(QsMemberExp memberExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            // TODO: 함수 지원, Lambda로 묶어주면 된다
            // TODO: AccessModifier
            if (!AnalyzeExpWithStatic(memberExp.Object, context, out var objTypeValue))
            {
                typeValue = null;
                return false;
            }

            // a.x, a.F
            var candidates = new List<(QsTypeValue TypeValue, QsStorage Storage)>();

            QsNormalTypeValue? objNormalTypeValue = objTypeValue.Value.TypeValue as QsNormalTypeValue;
            if (objNormalTypeValue == null)
            {
                context.Errors.Add((memberExp, "멤버를 가져올 수 없습니다"));
                typeValue = null;
                return false;
            }

            if (typeValueService.GetMemberVar(objNormalTypeValue.TypeId, memberExp.MemberName, context.TypeValueServiceContext, out var memberVar))
            {
                if (0 < memberExp.MemberTypeArgs.Length)
                    context.Errors.Add((memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다"));

                if (objTypeValue.Value.bStatic && !memberVar.Value.bStatic) // instance인데 static을 가져오는건 괜찮다
                    context.Errors.Add((memberExp, "정적 변수가 아닙니다"));

                typeValue = typeValueService.MakeTypeValue(objNormalTypeValue, memberVar.Value.Var.TypeValue, context.TypeValueServiceContext);

                var storage = memberVar.Value.bStatic 
                    ? (QsStorage)new QsStaticVarStorage(objNormalTypeValue, memberVar.Value.Var.VarId) 
                    : (QsStorage)new QsInstanceVarStorage(memberVar.Value.Var.VarId);

                candidates.Add((typeValue, storage));
            }

            if (typeValueService.GetMemberFunc(objNormalTypeValue.TypeId, new QsName(memberExp.MemberName), context.TypeValueServiceContext, out var memberFunc))
            {
                if (objTypeValue.Value.bStatic && !memberFunc.Value.bStatic) // instance인데 static을 가져오는건 괜찮다
                    context.Errors.Add((memberExp, "정적 함수가 아닙니다"));

                var typeArgs = ImmutableArray.CreateRange(memberExp.MemberTypeArgs, typeArg => context.TypeValuesByTypeExp[typeArg]);

                typeValue = typeValueService.MakeFuncTypeValue(objNormalTypeValue, memberFunc.Value.Func, typeArgs, context.TypeValueServiceContext);
                var storage = memberFunc.Value.bStatic
                    ? (QsStorage)new QsStaticFuncStorage(objNormalTypeValue, memberFunc.Value.Func.FuncId)
                    : (QsStorage)new QsInstanceFuncStorage(memberFunc.Value.Func.FuncId);

                candidates.Add((typeValue, storage));
            }
            
            if (candidates.Count == 1 )
            {
                context.TypeValuesByExp.Add(memberExp, candidates[0].TypeValue);
                context.EvalExpsByExp.Add(memberExp, candidates[0].Storage);
                typeValue = objTypeValue.Value.TypeValue;
                
                return true;
            }

            if (1 < candidates.Count)
                context.Errors.Add((memberExp, "이름이 여러 변수와 함수를 가리키고 있습니다"));
            else 
                context.Errors.Add((memberExp, $"{memberExp.MemberName} 이름을 가진 멤버가 없습니다"));

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
