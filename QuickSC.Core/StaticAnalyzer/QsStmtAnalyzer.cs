using QuickSC;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            analyzer.AnalyzeExp(ifStmt.Cond, context, out var condTypeValue);
            
            // if (x is X) 구문이 있으면 cond부분을 검사하지 않는다.
            if (ifStmt.TestType == null)
            {
                context.GlobalTypeIds.TryGetValue("bool", boolTypeValue);

                if (!IsAssignable(boolTypeValue, condTypeValue))
                {
                    context.Errors.Add((ifStmt, "if 조건 식은 항상 bool형식이어야 합니다"));
                }
            }

            AnalyzeStmt(ifStmt.Body, context);

            if (ifStmt.ElseBody != null)
                AnalyzeStmt(ifStmt.ElseBody, context);
        }

        void AnalyzeForStmt(QsForStmt forStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }

        void AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzerContext context)
        {
            // 아무것도 하지 않는다            
        }

        void AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzerContext context)
        {
            // 아무것도 하지 않는다
        }

        void AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        void AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzerContext context)
        { throw new NotImplementedException(); }

        void AnalyzeExpStmt(QsExpStmt expStmt, QsAnalyzerContext context)
        {
            analyzer.AnalyzeExp(expStmt.Exp, context, out var _);
        }

        void AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        void AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        void AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        void AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        void AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }

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
