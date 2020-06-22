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

        public QsStmtAnalyzer(QsAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        // CommandStmt에 있는 expStringElement를 분석한다
        bool AnalyzeCommandStmt(QsCommandStmt cmdStmt, QsAnalyzerContext context)
        {
            bool bResult = true;

            foreach (var cmd in cmdStmt.Commands)
                foreach (var elem in cmd.Elements)
                    bResult &= analyzer.AnalyzeStringExpElement(elem, context);

            return bResult;
        }

        bool AnalyzeVarDeclStmt(QsVarDeclStmt varDeclStmt, QsAnalyzerContext context)
        {
            return analyzer.AnalyzeVarDecl(varDeclStmt.VarDecl, context);
        }

        bool AnalyzeIfStmt(QsIfStmt ifStmt, QsAnalyzerContext context) 
        {
            bool bResult = true;

            if (!context.MetadataService.GetGlobalTypeValue("bool", out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            if (!analyzer.AnalyzeExp(ifStmt.Cond, context, out var condTypeValue))
                return false;
            
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
                context.InfosByNode[ifStmt] = new QsIfStmtInfo(context.TypeValuesByTypeExp[ifStmt.TestType]);
            }

            bResult &= AnalyzeStmt(ifStmt.Body, context);

            if (ifStmt.ElseBody != null)
                bResult &= AnalyzeStmt(ifStmt.ElseBody, context);

            return bResult;
        }

        bool AnalyzeForStmtInitializer(QsForStmtInitializer forInit, QsAnalyzerContext context)
        {
            switch(forInit)
            {
                case QsVarDeclForStmtInitializer varDeclInit: return analyzer.AnalyzeVarDecl(varDeclInit.VarDecl, context);
                case QsExpForStmtInitializer expInit: return analyzer.AnalyzeExp(expInit.Exp, context, out var _); 
                default: throw new NotImplementedException();
            }
        }

        bool AnalyzeForStmt(QsForStmt forStmt, QsAnalyzerContext context)
        {
            bool bResult = true;

            if (!context.MetadataService.GetGlobalTypeValue("bool", out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;

            if (forStmt.Initializer != null)
                bResult &= AnalyzeForStmtInitializer(forStmt.Initializer, context);

            if (forStmt.CondExp != null)
            {
                // 밑에서 쓰이므로 분석실패시 종료
                if (!analyzer.AnalyzeExp(forStmt.CondExp, context, out var condExpTypeValue))
                    return false;

                // 에러가 나면 에러를 추가하고 계속 진행
                if (!analyzer.IsAssignable(boolTypeValue, condExpTypeValue, context))
                    context.ErrorCollector.Add(forStmt.CondExp, $"{forStmt.CondExp}는 bool 형식이어야 합니다");
            }

            if (forStmt.ContinueExp != null)
                bResult &= analyzer.AnalyzeExp(forStmt.ContinueExp, context, out var _);

            bResult &= AnalyzeStmt(forStmt.Body, context);
            
            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);

            return bResult;
        }

        bool AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzerContext context)
        {
            // TODO: loop안에 있는지 확인한다
            return true;
        }

        bool AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzerContext context)
        {
            // loop안에 있는지 확인해야 한다
            return true;
        }
        
        bool AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzerContext context)
        {   
            if (returnStmt.Value != null)
            {
                if (context.CurFunc.bSequence)
                {
                    context.ErrorCollector.Add(returnStmt, $"seq 함수는 빈 return만 허용됩니다");
                    return false;
                }

                if (!analyzer.AnalyzeExp(returnStmt.Value, context, out var returnValueTypeValue))
                    return false; 
                
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
                if (!context.CurFunc.bSequence && context.CurFunc.RetTypeValue != QsTypeValue_Void.Instance)
                    context.ErrorCollector.Add(returnStmt.Value!, $"이 함수는 {context.CurFunc.RetTypeValue}을 반환해야 합니다");
            }

            return true;
        }

        bool AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzerContext context)
        {
            bool bResult = true;

            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;

            foreach (var stmt in blockStmt.Stmts)
            {
                bResult &= AnalyzeStmt(stmt, context);
            }

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);

            return bResult;
        }

        bool AnalyzeExpStmt(QsExpStmt expStmt, QsAnalyzerContext context)
        {
            bool bResult = true;

            if ((expStmt.Exp is QsUnaryOpExp unOpExp && (unOpExp.Kind != QsUnaryOpKind.PostfixInc || 
                    unOpExp.Kind != QsUnaryOpKind.PostfixDec ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixInc ||
                    unOpExp.Kind != QsUnaryOpKind.PrefixDec)) && 
                (expStmt.Exp is QsBinaryOpExp binOpExp && binOpExp.Kind != QsBinaryOpKind.Assign) &&
                !(expStmt.Exp is QsCallExp) &&
                !(expStmt.Exp is QsMemberCallExp))
            {
                context.ErrorCollector.Add(expStmt, "대입, 함수 호출만 구문으로 사용할 수 있습니다");
                bResult = false;
            }

            bResult &= analyzer.AnalyzeExp(expStmt.Exp, context, out var _);
            return bResult;
        }

        bool AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzerContext context)
        {
            if (!analyzer.AnalyzeLambda(taskStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return false;

            context.InfosByNode[taskStmt] = new QsTaskStmtInfo(captureInfo, localVarCount);
            return true;
        }

        bool AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzerContext context)
        {
            bool bResult = true;

            var (prevFunc, prevVarTypeValues, bPrevGlobalScope) = (context.CurFunc, context.CurFunc.GetVariables(), context.bGlobalScope);
            context.bGlobalScope = false;

            bResult &= AnalyzeStmt(awaitStmt.Body, context);

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);

            return bResult;
        }

        bool AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzerContext context)
        {
            if (!analyzer.AnalyzeLambda(asyncStmt.Body, ImmutableArray<QsLambdaExpParam>.Empty, context, out var captureInfo, out var funcTypeValue, out int localVarCount))
                return false;

            context.InfosByNode[asyncStmt] = new QsAsyncStmtInfo(captureInfo, localVarCount);
            return true;
        }
        
        bool AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzerContext context)
        {
            if (!context.MetadataService.GetGlobalTypeValue("bool", out var boolTypeValue))
                Debug.Fail("Runtime에 bool타입이 없습니다");

            if (!analyzer.AnalyzeExp(foreachStmt.Obj, context, out var objTypeValue))
                return false;

            var elemTypeValue = context.TypeValuesByTypeExp[foreachStmt.Type];

            if (!context.MetadataService.GetMemberFuncValue(
                false, objTypeValue,
                QsName.Text("GetEnumerator"), ImmutableArray<QsTypeValue>.Empty,
                out var getEnumeratorValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "foreach ... in 뒤 객체는 IEnumerator<T> GetEnumerator() 함수가 있어야 합니다.");
                return false;
            }

            // TODO: 일단 인터페이스가 없으므로, bool MoveNext()과 T GetCurrent()가 있는지 본다
            // TODO: 각 함수들이 thiscall인지도 확인해야 한다

            // 1. elemTypeValue가 VarTypeValue이면 GetEnumerator의 리턴값으로 판단한다
            var getEnumeratorTypeValue = context.MetadataService.GetFuncTypeValue(getEnumeratorValue);

            if (!context.MetadataService.GetMemberFuncValue(
                false, getEnumeratorTypeValue.Return,
                QsName.Text("MoveNext"), ImmutableArray<QsTypeValue>.Empty, out var moveNextValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return false;
            }

            var moveNextTypeValue = context.MetadataService.GetFuncTypeValue(moveNextValue);

            if (!analyzer.IsAssignable(boolTypeValue, moveNextTypeValue.Return, context))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function");
                return false;
            }

            if (!context.MetadataService.GetMemberFuncValue(
                false, getEnumeratorTypeValue.Return, 
                QsName.Text("GetCurrent"), ImmutableArray<QsTypeValue>.Empty, out var getCurrentValue))
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "enumerator doesn't have 'GetCurrent()' function");
                return false;
            }

            var getCurrentTypeValue = context.MetadataService.GetFuncTypeValue(getCurrentValue);
            if (getCurrentTypeValue.Return == QsTypeValue_Void.Instance)
            {
                context.ErrorCollector.Add(foreachStmt.Obj, "'GetCurrent()' function cannot return void");
                return false;
            }

            if (elemTypeValue == QsTypeValue_Var.Instance)
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

            bool bResult = AnalyzeStmt(foreachStmt.Body, context);

            Debug.Assert(prevFunc == context.CurFunc);
            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc.SetVariables(prevVarTypeValues);

            context.InfosByNode[foreachStmt] = new QsForeachStmtInfo(elemTypeValue, elemLocalIndex, getEnumeratorValue, moveNextValue, getCurrentValue);

            return bResult;
        }

        bool AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzerContext context)
        {
            if (!context.CurFunc.bSequence)
            {
                context.ErrorCollector.Add(yieldStmt, "seq 함수 내부에서만 yield를 사용할 수 있습니다");
                return false;
            }

            if (!analyzer.AnalyzeExp(yieldStmt.Value, context, out var yieldTypeValue))
                return false;

            // yield에서는 retType이 명시되는 경우만 있을 것이다
            Debug.Assert(context.CurFunc.RetTypeValue != null);

            if (!analyzer.IsAssignable(context.CurFunc.RetTypeValue, yieldTypeValue, context))
            {
                context.ErrorCollector.Add(yieldStmt.Value, $"반환 값의 {yieldTypeValue} 타입은 이 함수의 반환 타입과 맞지 않습니다");
                return false;
            }

            return true;
        }

        public bool AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            switch (stmt)
            {
                case QsCommandStmt cmdStmt: return AnalyzeCommandStmt(cmdStmt, context); 
                case QsVarDeclStmt varDeclStmt: return AnalyzeVarDeclStmt(varDeclStmt, context); 
                case QsIfStmt ifStmt: return AnalyzeIfStmt(ifStmt, context); 
                case QsForStmt forStmt: return AnalyzeForStmt(forStmt, context); 
                case QsContinueStmt continueStmt: return AnalyzeContinueStmt(continueStmt, context); 
                case QsBreakStmt breakStmt: return AnalyzeBreakStmt(breakStmt, context); 
                case QsReturnStmt returnStmt: return AnalyzeReturnStmt(returnStmt, context); 
                case QsBlockStmt blockStmt: return AnalyzeBlockStmt(blockStmt, context); 
                case QsBlankStmt _: return true;
                case QsExpStmt expStmt: return AnalyzeExpStmt(expStmt, context); 
                case QsTaskStmt taskStmt: return AnalyzeTaskStmt(taskStmt, context);
                case QsAwaitStmt awaitStmt: return AnalyzeAwaitStmt(awaitStmt, context); 
                case QsAsyncStmt asyncStmt: return AnalyzeAsyncStmt(asyncStmt, context); 
                case QsForeachStmt foreachStmt: return AnalyzeForeachStmt(foreachStmt, context); 
                case QsYieldStmt yieldStmt: return AnalyzeYieldStmt(yieldStmt, context); 
                default: throw new NotImplementedException();
            }
        }
    }
}
