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
        QsAnalyzerTypeService typeService;        

        public QsExpAnalyzer(QsAnalyzer analyzer, QsAnalyzerTypeService typeService)
        {
            this.analyzer = analyzer;
            this.typeService = typeService;
        }

        // x
        internal bool AnalyzeIdExp(QsIdentifierExp idExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            if (AnalyzeIdExpWithStatic(idExp, context, out var outValue))
            {
                if (!outValue.Value.bStatic)
                {
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
                    context.ErrorCollector.Add(idExp, $"{idExp.Value} 변수는 타입인자를 가질 수 없습니다");
                    outValue = null;
                    return false;
                }

                // Local
                context.InfosByNode.Add(idExp, new QsIdentifierExpInfo(new QsLocalStorage(localVarInfo.Index)));
                outValue = (false, localVarInfo.TypeValue);
                return true;
            }

            var typeArgs = ImmutableArray.CreateRange(idExp.TypeArgs, typeArg => context.TypeBuildInfo.TypeValuesByTypeExp[typeArg]);

            // TODO: this scope 변수, 함수 검색            

            // 전역 변수/함수, 레퍼런스에서 검색
            var candidates = new List<(QsTypeValue TypeValue, QsIdentifierExpInfo Info)>();
            if (idExp.TypeArgs.Length == 0 && typeService.GetGlobalVar(idExp.Value, context, out var globalVar))
            {
                // GlobalVar
                candidates.Add((globalVar.TypeValue, new QsIdentifierExpInfo(new QsGlobalStorage(globalVar.VarId))));
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
                context.InfosByNode.Add(idExp, candidates[0].Info);
                outValue = (false, candidates[0].TypeValue);
                return true;
            }
            else if (1 < candidates.Count)
            {
                outValue = null;
                context.ErrorCollector.Add(idExp, $"{idExp}가 모호합니다. 가능한 함수, 람다가 여러개 있습니다");
                return false;
            }

            // 전역/레퍼런스 타입에서 검색, GlobalType, RefType
            if (typeService.GetGlobalTypeValue(idExp.Value, typeArgs, context, out typeValue))
            {
                // 여기는 MemberExp로부터만 오기 때문에 MemberExp에 추가해본다
                outValue = (true, typeValue);
                return true;
            }

            outValue = null;
            context.ErrorCollector.Add(idExp, "가능한 함수, 람다가 없습니다");
            return false;
        }

        // identifier가 type을 가리킬 때, AnalyzeExp는 에러를 추가해서 진행을 멈추지만 AnalyzeExpWithStatic은 계속 진행할 수 있습니다
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
            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue))
                Debug.Fail("Runtime에 bool이 없습니다");

            typeValue = boolTypeValue;
            return true;
        }

        internal bool AnalyzeIntLiteralExp(QsIntLiteralExp intExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            if (!typeService.GetGlobalTypeValue("int", context, out var intTypeValue))
                Debug.Fail("Runtime에 int가 없습니다");

            typeValue = intTypeValue;
            return true;
        }

        internal bool AnalyzeStringExp(QsStringExp stringExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            if (!typeService.GetGlobalTypeValue("string", context, out var stringTypeValue))
                Debug.Fail("Runtime에 string이 없습니다");

            typeValue = stringTypeValue;
            return true;
        }

        internal bool AnalyzeUnaryOpExp(QsUnaryOpExp unaryOpExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            // TODO: operator 함수 선택 방식 따로 만들기, 지금은 하드코딩
            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue) || 
                !typeService.GetGlobalTypeValue("int", context, out var intTypeValue))
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

            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue) ||
                !typeService.GetGlobalTypeValue("int", context, out var intTypeValue) ||
                !typeService.GetGlobalTypeValue("string", context, out var stringTypeValue))
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
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue1}를 {operandTypeValue0}에 대입할 수 없습니다");
                    return false;
                }

                typeValue = operandTypeValue0;
                return true;
            }
            else if (binaryOpExp.Kind == QsBinaryOpKind.Equal || binaryOpExp.Kind == QsBinaryOpKind.NotEqual)
            {
                // TODO: 비교가능함은 어떻게 하나
                if (operandTypeValue0 != operandTypeValue1)
                {
                    context.ErrorCollector.Add(binaryOpExp, $"{operandTypeValue0}와 {operandTypeValue1}을 비교할 수 없습니다");
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
                context.InfosByNode[binaryOpExp] = new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.Integer);

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
                context.InfosByNode[binaryOpExp] = new QsBinaryOpExpInfo(QsBinaryOpExpInfo.OpType.String);

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
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsFuncTypeValue TypeValue)? outValue)
        {
            // 1. this 검색

            // 2. global 검색
            if (typeService.GetGlobalFunc(exp.Value, exp.TypeArgs.Length, context, out var globalFunc))
            {
                var typeArgs = ImmutableArray.CreateRange(exp.TypeArgs, typeArg => context.TypeBuildInfo.TypeValuesByTypeExp[typeArg]);

                var funcValue = new QsFuncValue(null, globalFunc.FuncId, typeArgs);
                var funcTypeValue = typeService.GetFuncTypeValue(funcValue, context);

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
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsFuncTypeValue TypeValue)? outValue)
        {
            if (!AnalyzeExp(exp, context, out var typeValue))
            {
                outValue = null;
                return false;
            }

            var funcTypeValue = typeValue as QsFuncTypeValue;
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
            [NotNullWhen(returnValue: true)] out (QsFuncValue? FuncValue, QsFuncTypeValue TypeValue)? outValue)
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
            context.InfosByNode[exp] = new QsCallExpInfo(callableInfo.Value.FuncValue);
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
            context.InfosByNode[lambdaExp] = new QsLambdaExpInfo(captureInfo, localVarCount);
            return true;
        }

        ImmutableArray<QsTypeValue> GetTypeValues(ImmutableArray<QsTypeExp> typeExps, QsAnalyzerContext context)
        {
            return ImmutableArray.CreateRange(typeExps, typeExp => context.TypeBuildInfo.TypeValuesByTypeExp[typeExp]);
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

        internal bool AnalyzeMemberCallExp(QsMemberCallExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue) 
        {
            typeValue = null;

            var memberTypeArgs = GetTypeValues(exp.MemberTypeArgs, context);

            if (!AnalyzeExps(exp.Args, context, out var args))
                return false;

            // 가능한 경우 'obj'.F(), 'obj'.f(), 'C'.F() 'C'.f()
            if (!AnalyzeExpWithStatic(exp.Object, context, out var outValue))
                return false;

            var (bStaticObject, objTypeValue) = outValue.Value;
            
            // 함수에서 찾기.. FuncValue도 같이 주는것이 좋을 듯 하다
            if (typeService.GetMemberFuncValue(bStaticOnly: bStaticObject, objTypeValue, QsName.Text(exp.MemberName), memberTypeArgs, context, out var funcValue))
            {
                bool bStaticFunc = typeService.IsFuncStatic(funcValue.FuncId, context);
                var funcTypeValue = typeService.GetFuncTypeValue(funcValue, context);

                if (!CheckParamTypes(exp, funcTypeValue.Params, args, context))
                    return false;

                context.InfosByNode[exp] = bStaticFunc
                    ? QsMemberCallExpInfo.MakeStaticFunc(bEvaluateObject: !bStaticObject, funcValue)
                    : QsMemberCallExpInfo.MakeInstanceFunc(funcValue);
                
                typeValue = funcTypeValue.Return;
                return true;
            }

            // 변수에서 찾기
            if (memberTypeArgs.Length == 0 && 
                typeService.GetMemberVarValue(bStaticOnly: bStaticObject, objTypeValue, exp.MemberName, context, out var varValue))
            {
                bool bStaticVar = typeService.IsVarStatic(varValue.VarId, context);
                var varFuncTypeValue = typeService.GetVarTypeValue(varValue, context) as QsFuncTypeValue;

                if (varFuncTypeValue == null)
                {
                    context.ErrorCollector.Add(exp, $"호출 가능한 타입이 아닙니다");
                    return false;
                }

                context.InfosByNode[exp] = bStaticVar
                    ? QsMemberCallExpInfo.MakeStaticLambda(bEvaluateObject: !bStaticObject, varValue)
                    : QsMemberCallExpInfo.MakeInstanceLambda(varValue.VarId);

                typeValue = varFuncTypeValue.Return;
                return true;
            }

            // 변수에서 찾기 VarId도 같이 주는것이 좋을 것 같다
            context.ErrorCollector.Add(exp, $"{exp.Object}에 {exp.MemberName} 함수가 없습니다");
            return false;
        }

        internal bool AnalyzeMemberExp(QsMemberExp memberExp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue) 
        {
            if (!AnalyzeExpWithStatic(memberExp.Object, context, out var objInfo))
            {
                outTypeValue = null;
                return false;
            }

            // obj.id
            if (!objInfo.Value.bStatic)
            {
                QsNormalTypeValue? objNormalTypeValue = objInfo.Value.TypeValue as QsNormalTypeValue;

                if (objNormalTypeValue == null)
                {
                    context.ErrorCollector.Add(memberExp, "멤버를 가져올 수 없습니다");
                    outTypeValue = null;
                    return false;
                }

                if (!typeService.GetMemberVar(objNormalTypeValue.TypeId, memberExp.MemberName, context, out var memberVar))
                {
                    outTypeValue = null;
                    return false;
                }

                if (0 < memberExp.MemberTypeArgs.Length)
                    context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");

                outTypeValue = typeService.MakeTypeValue(objNormalTypeValue, memberVar.Value.Var.TypeValue, context);

                // instance이지만 static 이라면, exp는 실행하고, static변수에서 가져온다
                if (memberVar.Value.bStatic)
                {
                    context.InfosByNode[memberExp] = QsMemberExpInfo.MakeStatic(true, new QsVarValue(objNormalTypeValue, memberVar.Value.Var.VarId));
                    return true;
                }
                else
                {
                    context.InfosByNode[memberExp] = QsMemberExpInfo.MakeInstance(memberVar.Value.Var.VarId);
                    return true;
                }
            }
            else // X.id
            {
                QsNormalTypeValue? objNormalTypeValue = objInfo.Value.TypeValue as QsNormalTypeValue;

                if (objNormalTypeValue == null)
                {
                    context.ErrorCollector.Add(memberExp, "멤버를 가져올 수 없습니다");
                    outTypeValue = null;
                    return false;
                }

                if (typeService.GetMemberVar(objNormalTypeValue.TypeId, memberExp.MemberName, context, out var variable))
                {
                    if (0 < memberExp.MemberTypeArgs.Length)
                    {
                        context.ErrorCollector.Add(memberExp, "멤버변수에는 타입인자를 붙일 수 없습니다");
                        outTypeValue = null;
                        return false;
                    }

                    if (!variable.Value.bStatic) // instance인데 static을 가져오는건 괜찮다
                    {
                        context.ErrorCollector.Add(memberExp, "정적 변수가 아닙니다");
                        outTypeValue = null;
                        return false;
                    }
                    else
                    {
                        outTypeValue = typeService.MakeTypeValue(objNormalTypeValue, variable.Value.Var.TypeValue, context);
                        context.InfosByNode[memberExp] = QsMemberExpInfo.MakeStatic(false, new QsVarValue(objNormalTypeValue, variable.Value.Var.VarId));
                        return true;
                    }                    
                }
            }

            // TODO: Func추가

            outTypeValue = null;
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
                    context.ErrorCollector.Add(listExp, $"원소 {elem}의 타입이 {curElemTypeValue} 가 아닙니다");
                    return false;
                }
            }

            if (curElemTypeValue == null)
            {
                context.ErrorCollector.Add(listExp, $"리스트의 타입을 결정하지 못했습니다");
                return false;
            }

            if (!typeService.GetGlobalTypeValue("List", ImmutableArray.Create(curElemTypeValue), context, out typeValue))
            {
                Debug.Fail("Runtime에 리스트가 없습니다");
                return false;
            }
            
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
