using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    class QsStmtAnalyzer
    {
        QsAnalyzer analyzer;
        QsTypeValueService typeValueService;

        public QsStmtAnalyzer(QsAnalyzer analyzer, QsTypeValueService typeValueService)
        {
            this.analyzer = analyzer;
            this.typeValueService = typeValueService;
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
            if (!analyzer.AnalyzeExp(ifStmt.Cond, context, out var condTypeValue))            
                return;            
            
            // if (x is X) 구문이 있으면 cond부분을 검사하지 않는다.
            if (ifStmt.TestType == null)
            {
                if (!IsAssignable(context.BoolTypeValue, condTypeValue))
                {
                    context.Errors.Add((ifStmt, "if 조건 식은 항상 bool형식이어야 합니다"));
                }
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
            if (forStmt.Initializer != null)
                AnalyzeForStmtInitializer(forStmt.Initializer, context);

            if (forStmt.CondExp != null)
            {
                // 밑에서 쓰이므로 분석실패시 종료
                if (!analyzer.AnalyzeExp(forStmt.CondExp, context, out var condExpTypeValue))
                    return;

                // 에러가 나면 에러를 추가하고 계속 진행
                if (!IsAssignable(context.BoolTypeValue, condExpTypeValue))
                    context.Errors.Add((forStmt.CondExp, $"{forStmt.CondExp}는 bool 형식이어야 합니다"));
            }

            if (forStmt.ContinueExp != null)
                analyzer.AnalyzeExp(forStmt.ContinueExp, context, out var _);

            analyzer.AnalyzeStmt(forStmt.Body, context);
        }

        void AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzerContext context)
        {
            // 아무것도 하지 않는다            
        }

        void AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzerContext context)
        {
            // 아무것도 하지 않는다
        }
        
        void AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzerContext context)
        {
            if (context.CurFunc == null)
            {
                context.Errors.Add((returnStmt, $"함수 내부가 아닌곳에서 return을 사용했습니다"));
                return;
            }
            
            if (returnStmt.Value != null)
            {
                if (!analyzer.AnalyzeExp(returnStmt.Value, context, out var returnValueTypeValue))
                    return;

                // 현재 함수 시그니처랑 맞춰서 같은지 확인한다
                if (!IsAssignable(context.CurFunc.RetTypeValue, returnValueTypeValue))
                    context.Errors.Add((returnStmt.Value, $"반환값의 타입 {returnValueTypeValue}는 이 함수의 반환타입과 맞지 않습니다"));
            }
            else
            {
                // TODO: seq 함수는 그냥 리턴이 허용된다.

                if (context.CurFunc.RetTypeValue != context.VoidTypeValue)
                    context.Errors.Add((returnStmt.Value!, $"이 함수는 {context.CurFunc.RetTypeValue}을 반환해야 합니다"));
            }
        }

        void AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzerContext context)
        {
            var prevVarTypeValues = context.VarTypeValues;

            foreach(var stmt in blockStmt.Stmts)
            {
                AnalyzeStmt(stmt, context);
            }

            context.VarTypeValues = prevVarTypeValues;
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
                context.Errors.Add((expStmt, "대입, 함수 호출만 구문으로 사용할 수 있습니다"));
            }

            analyzer.AnalyzeExp(expStmt.Exp, context, out var _);            
        }

        void AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzerContext context)
        {
            var captureContext = QsCaptureContext.Make();

            // TODO: Capture로 순회를 따로 할 필요 없이, Analyze에서 같이 할 수도 있을 것 같다
            if (!analyzer.CaptureStmt(taskStmt.Body, ref captureContext))
                context.Errors.Add((taskStmt, "변수 캡쳐에 실패했습니다"));

            context.CaptureInfosByLocation.Add(QsCaptureInfoLocation.Make(taskStmt), captureContext.NeedCaptures);

            var prevVarTypeValues = context.VarTypeValues;

            AnalyzeStmt(taskStmt.Body, context);

            context.VarTypeValues = prevVarTypeValues;
        }

        void AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzerContext context)
        {
            var captureContext = QsCaptureContext.Make();

            // TODO: Capture로 순회를 따로 할 필요 없이, Analyze에서 같이 할 수도 있을 것 같다
            if (!analyzer.CaptureStmt(awaitStmt.Body, ref captureContext))
                context.Errors.Add((awaitStmt, "변수 캡쳐에 실패했습니다"));

            context.CaptureInfosByLocation.Add(QsCaptureInfoLocation.Make(awaitStmt), captureContext.NeedCaptures);

            // TODO: 스코프 내에 await 할 것들이 있는지 검사.. 
            var prevVarTypeValues = context.VarTypeValues;

            AnalyzeStmt(awaitStmt.Body, context);

            context.VarTypeValues = prevVarTypeValues;
        }

        void AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzerContext context)
        {
            // TODO: 스코프 내에 await 할 것들이 있는지 검사.. 
            var prevVarTypeValues = context.VarTypeValues;

            AnalyzeStmt(asyncStmt.Body, context);

            context.VarTypeValues = prevVarTypeValues;
        }

        bool GetFuncTypeValue(bool bStaticOnly, QsTypeValue typeValue, QsMemberFuncId memberFuncId, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            return typeValueService.GetFuncTypeValue(bStaticOnly, typeValue, memberFuncId, ImmutableArray<QsTypeValue>.Empty, context.TypeValueServiceContext, out funcTypeValue);
        }

        bool GetReturnTypeValue(QsFuncTypeValue funcTypeValue, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? retTypeValue)
        {
            return typeValueService.GetReturnTypeValue(funcTypeValue, context.TypeValueServiceContext, out retTypeValue);
        }

        void AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzerContext context)
        {
            if (!analyzer.AnalyzeExp(foreachStmt.Obj, context, out var objTypeValue))
                return;

            var elemTypeValue = context.TypeValuesByTypeExp[foreachStmt.Type];

            // 1. elemTypeValue가 VarTypeValue이면 GetEnumerator의 리턴값으로 판단한다
            if (!GetFuncTypeValue(false, objTypeValue, new QsMemberFuncId("GetEnumerator"), context, out var getEnumeratorTypeValue))
            {
                context.Errors.Add((foreachStmt.Obj, "foreach ... in 뒤 객체는 IEnumerator<T> GetEnumerator() 함수가 있어야 합니다."));
                return;
            }

            if (!GetReturnTypeValue(getEnumeratorTypeValue, context, out var getEnumeratorRetTypeValue))
                return;

            // TODO: 일단 인터페이스가 없으므로, bool MoveNext()과 T GetCurrent()가 있는지 본다
            // TODO: thiscall인지도 확인해야 한다
            if (!GetFuncTypeValue(false, getEnumeratorRetTypeValue, new QsMemberFuncId("MoveNext"), context, out var moveNextTypeValue) ||
                !GetReturnTypeValue(moveNextTypeValue, context, out var moveNextRetTypeValue) || 
                !IsAssignable(context.BoolTypeValue, moveNextRetTypeValue))
            {
                context.Errors.Add((foreachStmt.Obj, "enumerator doesn't have 'bool MoveNext()' function"));
                return;
            }

            if (!GetFuncTypeValue(false, getEnumeratorRetTypeValue, new QsMemberFuncId("GetCurrent"), context, out var getCurrentTypeValue))
            {
                context.Errors.Add((foreachStmt.Obj, "enumerator doesn't have 'GetCurrent()' function"));
                return;
            }

            if (!GetReturnTypeValue(getCurrentTypeValue, context, out var getCurrentRetTypeValue))
                return;

            if (elemTypeValue == QsVarTypeValue.Instance)
            {   
                elemTypeValue = getCurrentRetTypeValue;
                context.TypeValuesByTypeExp[foreachStmt.Type] = elemTypeValue;

                //var interfaces = typeValueService.GetInterfaces("IEnumerator", 1, funcTypeValue.RetTypeValue);

                //if (1 < interfaces.Count)
                //{
                //    context.Errors.Add((foreachStmt.Obj, "변수 타입으로 var를 사용하였는데, IEnumerator<T>가 여러개라 어느 것을 사용할지 결정할 수 없습니다."));
                //    return;
                //}
            }
            else
            {
                if (!IsAssignable(elemTypeValue, getCurrentRetTypeValue))
                    context.Errors.Add((foreachStmt, $"foreach(T ... in obj) 에서 obj.GetEnumerator().GetCurrent()의 결과를 {elemTypeValue} 타입으로 캐스팅할 수 없습니다"));
            }

            var prevVarTypeValues = context.VarTypeValues;
            context.VarTypeValues.Add(foreachStmt.VarName, elemTypeValue);

            AnalyzeStmt(foreachStmt.Body, context);

            context.VarTypeValues = prevVarTypeValues;
        }

        void AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzerContext context)
        {
            if (context.CurFunc == null)
            {
                context.Errors.Add((yieldStmt, "seq 함수 내부에서만 yield를 사용할 수 있습니다"));
                return;
            }

            if (!analyzer.AnalyzeExp(yieldStmt.Value, context, out var yieldTypeValue))
                return;

            if (!IsAssignable(context.CurFunc.RetTypeValue, yieldTypeValue))
                context.Errors.Add((yieldStmt.Value, $"반환 값의 {yieldTypeValue} 타입은 이 함수의 반환 타입과 맞지 않습니다"));
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
