using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    class QsStmtAnalyzer
    {
        QsAnalyzer analyzer;
        QsAnalyzerTypeService typeService;

        public QsStmtAnalyzer(QsAnalyzer analyzer, QsAnalyzerTypeService typeService)
        {
            this.analyzer = analyzer;
            this.typeService = typeService;
        }

        // CommandStmt에 있는 expStringElement를 분석한다
        void AnalyzeCommandStmt(QsCommandStmt cmdStmt, QsAnalyzerContext context)
        {
            foreach (var cmd in cmdStmt.Commands)
                foreach (var elem in cmd.Elements)
                {
                    if (elem is QsExpStringExpElement expElem)
                    {
                        // TODO: exp의 결과 string으로 변환 가능해야 하는 조건도 고려해야 한다
                        analyzer.AnalyzeExp(expElem.Exp, context, out var _);
                    }
                }
        }

        void AnalyzeVarDeclStmt(QsVarDeclStmt varDeclStmt, QsAnalyzerContext context)
        {
            analyzer.AnalyzeVarDecl(varDeclStmt.VarDecl, context);
        }

        void AnalyzeIfStmt(QsIfStmt ifStmt, QsAnalyzerContext context) 
        {
            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            if (!analyzer.AnalyzeExp(ifStmt.Cond, context, out var condTypeValue))            
                return;            
            
            // if (x is X) 구문이 있으면 cond부분을 검사하지 않는다.
            if (ifStmt.TestType == null)
            {
                if (!analyzer.IsAssignable(boolTypeValue, condTypeValue, context))
                {
                    context.ErrorCollector.Add(ifStmt, "if 조건 식은 항상 bool형식이어야 합니다");
                }
            }
            else
            {
                // TestType이 있을때만 넣는다
                context.InfosByNode[ifStmt] = new QsIfStmtInfo(context.TypeBuildInfo.TypeValuesByTypeExp[ifStmt.TestType]);
            }

            AnalyzeStmt(ifStmt.Body, context);

            if (ifStmt.ElseBody != null)
                AnalyzeStmt(ifStmt.ElseBody, context);
        }

        void AnalyzeForStmtInitializer(QsForStmtInitializer forInit, QsAnalyzerContext context)
        {
            switch(forInit)
            {
                case QsVarDeclForStmtInitializer varDeclInit: analyzer.AnalyzeVarDecl(varDeclInit.VarDecl, context); break;
                case QsExpForStmtInitializer expInit: analyzer.AnalyzeExp(expInit.Exp, context, out var _); break;
                default: throw new NotImplementedException();
            }
        }

        void AnalyzeForStmt(QsForStmt forStmt, QsAnalyzerContext context)
        {
            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            if (forStmt.Initializer != null)
                AnalyzeForStmtInitializer(forStmt.Initializer, context);

            if (forStmt.CondExp != null)
            {
                // 밑에서 쓰이므로 분석실패시 종료
                if (!analyzer.AnalyzeExp(forStmt.CondExp, context, out var condExpTypeValue))
                    return;

                // 에러가 나면 에러를 추가하고 계속 진행
                if (!analyzer.IsAssignable(boolTypeValue, condExpTypeValue, context))
                    context.ErrorCollector.Add(forStmt.CondExp, $"{forStmt.CondExp}는 bool 형식이어야 합니다");
            }

            if (forStmt.ContinueExp != null)
                analyzer.AnalyzeExp(forStmt.ContinueExp, context, out var _);

            analyzer.AnalyzeStmt(forStmt.Body, context);
        }

        void AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzerContext context)
        {
            // loop안에 있는지 확인한다
        }

        void AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzerContext context)
        {
            // loop안에 있는지 확인해야 한다
        }
        
        void AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzerContext context)
        {   
            if (returnStmt.Value != null)
            {
                if (context.CurFunc.bSequence)
                {
                    context.ErrorCollector.Add(returnStmt, $"seq 함수는 빈 return만 허용됩니다");
                    return;
                }

                if (!analyzer.AnalyzeExp(returnStmt.Value, context, out var returnValueTypeValue))
                    return;
                
                if (context.CurFunc.RetTypeValue != null)
                {
                    // 현재 함수 시그니처랑 맞춰서 같은지 확인한다
                    if (!analyzer.IsAssignable(context.CurFunc.RetTypeValue, returnValueTypeValue, context))
                        context.ErrorCollector.Add(returnStmt.Value, $"반환값의 타입 {returnValueTypeValue}는 이 함수의 반환타입과 맞지 않습니다");
                }
                else // 리턴타입이 정해지지 않았을 경우가 있다
                {
                    context.CurFunc.RetTypeValue = returnValueTypeValue;
                }
            }
            else
            {
                if (!context.CurFunc.bSequence && context.CurFunc.RetTypeValue != QsVoidTypeValue.Instance)
                    context.ErrorCollector.Add(returnStmt.Value!, $"이 함수는 {context.CurFunc.RetTypeValue}을 반환해야 합니다");
            }
        }

        void AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzerContext context)
        {
            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;

            foreach (var stmt in blockStmt.Stmts)
            {
                AnalyzeStmt(stmt, context);
            }

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);
        }

        void AnalyzeExpStmt(QsExpStmt expStmt, QsAnalyzerContext context)
        {
            if ((expStmt.Exp is QsUnaryOpExp unOpExp && (unOpExp.Kind != QsUnaryOpKind.PostfixInc || 
                    unOpExp.Kind != QsUnaryOpKind.PostfixDec ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixInc ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixDec)) && 
                (expStmt.Exp is QsBinaryOpExp binOpExp && binOpExp.Kind != QsBinaryOpKind.Assign) &&
                !(expStmt.Exp is QsCallExp) &&
                !(expStmt.Exp is QsMemberCallExp))
            {
                context.ErrorCollector.Add(expStmt, "대입, 함수 호출만 구문으로 사용할 수 있습니다");
            }

            analyzer.AnalyzeExp(expStmt.Exp, context, out var _);            
        }

        void AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzerContext context)
        {
            if (!analyzer.AnalyzeLambda(taskStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return;

            context.InfosByNode[taskStmt] = new QsTaskStmtInfo(captureInfo, localVarCount);
        }

        void AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzerContext context)
        {
            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;

            AnalyzeStmt(awaitStmt.Body, context);

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);
        }

        void AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzerContext context)
        {
            if (!analyzer.AnalyzeLambda(asyncStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return;

            context.InfosByNode[asyncStmt] = new QsAsyncStmtInfo(captureInfo, localVarCount);
        }
        
        void AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzerContext context)
        {
            if (!typeService.GetGlobalTypeValue("bool", context, out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            if (!analyzer.AnalyzeExp(foreachStmt.Obj, context, out var objTypeValue))
                return;

            var elemTypeValue = context.TypeBuildInfo.TypeValuesByTypeExp[foreachStmt.Type];

            if (!typeService.GetMemberFuncValue(
                false, objTypeValue,
                QsName.Text("GetEnumerator"), ImmutableArray<QsTypeValue>.Empty,
                context, out var getEnumeratorValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "foreach ... in 뒤 객체는 IEnumerator<T> GetEnumerator() 함수가 있어야 합니다.");
                return;
            }

            // TODO: 일단 인터페이스가 없으므로, bool MoveNext()과 T GetCurrent()가 있는지 본다
            // TODO: 각 함수들이 thiscall인지도 확인해야 한다

            // 1. elemTypeValue가 VarTypeValue이면 GetEnumerator의 리턴값으로 판단한다
            var getEnumeratorTypeValue = typeService.GetFuncTypeValue(getEnumeratorValue, context);

            if (!typeService.GetMemberFuncValue(
                false, getEnumeratorTypeValue.Return,
                QsName.Text("MoveNext"), ImmutableArray<QsTypeValue>.Empty, context, out var moveNextValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return;
            }

            var moveNextTypeValue = typeService.GetFuncTypeValue(moveNextValue, context);

            if (!analyzer.IsAssignable(boolTypeValue, moveNextTypeValue.Return, context))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return;
            }

            if (!typeService.GetMemberFuncValue(
                false, getEnumeratorTypeValue.Return, 
                QsName.Text("GetCurrent"), ImmutableArray<QsTypeValue>.Empty, context, out var getCurrentValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'GetCurrent()' function");
                return;
            }

            var getCurrentTypeValue = typeService.GetFuncTypeValue(getCurrentValue, context);
            if (getCurrentTypeValue.Return == QsVoidTypeValue.Instance)
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "'GetCurrent()' function cannot return void");
                return;
            }

            if (elemTypeValue == QsVarTypeValue.Instance)
            {   
                elemTypeValue = getCurrentTypeValue.Return;

                //var interfaces = typeValueService.GetInterfaces("IEnumerator", 1, funcTypeValue.RetTypeValue);

                //if (1 < interfaces.Count)
                //{
                //    context.ErrorCollector.Add(foreachStmt.Obj, "변수 타입으로 var를 사용하였는데, IEnumerator<T>가 여러개라 어느 것을 사용할지 결정할 수 없습니다.");
                //    return;
                //}
            }
            else
            {
                if (!analyzer.IsAssignable(elemTypeValue, getCurrentTypeValue.Return, context))
                    context.ErrorCollector.Add(foreachStmt, $"foreach(T ... in obj) 에서 obj.GetEnumerator().GetCurrent()의 결과를 {elemTypeValue} 타입으로 캐스팅할 수 없습니다");
            }

            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;
            int elemLocalIndex = context.CurFunc.AddVarInfo(foreachStmt.VarName, elemTypeValue);
            
            AnalyzeStmt(foreachStmt.Body, context);

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);

            context.InfosByNode[foreachStmt] = new QsForeachStmtInfo(elemTypeValue, elemLocalIndex, getEnumeratorValue, moveNextValue, getCurrentValue);
        }

        void AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzerContext context)
        {
            if (!context.CurFunc.bSequence)
            {
                context.ErrorCollector.Add(yieldStmt, "seq 함수 내부에서만 yield를 사용할 수 있습니다");
                return;
            }

            if (!analyzer.AnalyzeExp(yieldStmt.Value, context, out var yieldTypeValue))
                return;

            // yield에서는 retType이 명시되는 경우만 있을 것이다
            Debug.Assert(context.CurFunc.RetTypeValue != null);

            if (!analyzer.IsAssignable(context.CurFunc.RetTypeValue, yieldTypeValue, context))
                context.ErrorCollector.Add(yieldStmt.Value, $"반환 값의 {yieldTypeValue} 타입은 이 함수의 반환 타입과 맞지 않습니다");
        }

        public void AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: AnalyzeCommandStmt(cmdStmt, context); break;
                case QsVarDeclStmt varDeclStmt: AnalyzeVarDeclStmt(varDeclStmt, context); break;
                case QsIfStmt ifStmt: AnalyzeIfStmt(ifStmt, context); break;
                case QsForStmt forStmt: AnalyzeForStmt(forStmt, context); break;
                case QsContinueStmt continueStmt: AnalyzeContinueStmt(continueStmt, context); break;
                case QsBreakStmt breakStmt: AnalyzeBreakStmt(breakStmt, context); break;
                case QsReturnStmt returnStmt: AnalyzeReturnStmt(returnStmt, context); break;
                case QsBlockStmt blockStmt: AnalyzeBlockStmt(blockStmt, context); break;
                case QsExpStmt expStmt: AnalyzeExpStmt(expStmt, context); break;
                case QsTaskStmt taskStmt: AnalyzeTaskStmt(taskStmt, context); break;
                case QsAwaitStmt awaitStmt: AnalyzeAwaitStmt(awaitStmt, context); break;
                case QsAsyncStmt asyncStmt: AnalyzeAsyncStmt(asyncStmt, context); break;
                case QsForeachStmt foreachStmt: AnalyzeForeachStmt(foreachStmt, context); break;
                case QsYieldStmt yieldStmt: AnalyzeYieldStmt(yieldStmt, context); break;
                default: throw new NotImplementedException();
            }
        }
    }
}
