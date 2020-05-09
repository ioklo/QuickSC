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
        QsAnalyzeResult AnalyzeCommandStmt(QsCommandStmt cmdStmt, QsAnalyzerContext context)
        {
            QsAnalyzeResult result;

            foreach(var cmd in cmdStmt.Commands)
                foreach(var elem in cmd.Elements)
                {
                    if (elem is QsExpStringExpElement expElem)
                    {
                        // TODO: exp의 결과 string으로 변환 가능해야 하는 조건도 고려해야 한다

                        var elemResult = analyzer.AnalyzeExp(expElem.Exp, context);
                        result.Merge(elemResult);
                    }
                }

            return result;
        }        

        QsAnalyzeResult AnalyzeVarDeclStmt(QsVarDeclStmt varDeclStmt, QsAnalyzerContext context) 
        {
            return analyzer.AnalyzeVarDecl(varDeclStmt.VarDecl, context);
        }

        QsAnalyzeResult AnalyzeIfStmt(QsIfStmt ifStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeForStmt(QsForStmt forStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        
        QsAnalyzeResult AnalyzeContinueStmt(QsContinueStmt continueStmt, QsAnalyzerContext context) 
        {
            // 아무것도 하지 않는다
            return QsAnalyzeResult.OK;
        }

        QsAnalyzeResult AnalyzeBreakStmt(QsBreakStmt breakStmt, QsAnalyzerContext context)
        {
            return QsAnalyzeResult.OK;
        }

        QsAnalyzeResult AnalyzeReturnStmt(QsReturnStmt returnStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeBlockStmt(QsBlockStmt blockStmt, QsAnalyzerContext context) 
        { throw new NotImplementedException(); }

        QsAnalyzeResult AnalyzeExpStmt(QsExpStmt expStmt, QsAnalyzerContext context) 
        {
            return analyzer.AnalyzeExp(expStmt.Exp, context);
        }

        QsAnalyzeResult AnalyzeTaskStmt(QsTaskStmt taskStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeAwaitStmt(QsAwaitStmt awaitStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeAsyncStmt(QsAsyncStmt asyncStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeForeachStmt(QsForeachStmt foreachStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }
        QsAnalyzeResult AnalyzeYieldStmt(QsYieldStmt yieldStmt, QsAnalyzerContext context) { throw new NotImplementedException(); }

        public QsAnalyzeResult AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            return stmt switch
            {
                QsCommandStmt cmdStmt => AnalyzeCommandStmt(cmdStmt, context),
                QsVarDeclStmt varDeclStmt => AnalyzeVarDeclStmt(varDeclStmt, context),
                QsIfStmt ifStmt => AnalyzeIfStmt(ifStmt, context),
                QsForStmt forStmt => AnalyzeForStmt(forStmt, context),
                QsContinueStmt continueStmt => AnalyzeContinueStmt(continueStmt, context),
                QsBreakStmt breakStmt => AnalyzeBreakStmt(breakStmt, context),
                QsReturnStmt returnStmt => AnalyzeReturnStmt(returnStmt, context),
                QsBlockStmt blockStmt => AnalyzeBlockStmt(blockStmt, context),
                QsExpStmt expStmt => AnalyzeExpStmt(expStmt, context),
                QsTaskStmt taskStmt => AnalyzeTaskStmt(taskStmt, context),
                QsAwaitStmt awaitStmt => AnalyzeAwaitStmt(awaitStmt, context),
                QsAsyncStmt asyncStmt => AnalyzeAsyncStmt(asyncStmt, context),
                QsForeachStmt foreachStmt => AnalyzeForeachStmt(foreachStmt, context),
                QsYieldStmt yieldStmt => AnalyzeYieldStmt(yieldStmt, context),

                _ => throw new NotImplementedException()
            };
        }
    }
}
