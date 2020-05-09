using System;
using System.Collections.Generic;
using System.Text;

using QuickSC.Syntax;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    class QsAnalyzer
    {
        QsStmtAnalyzer stmtAnalyzer;

        public QsAnalyzer()
        {
            stmtAnalyzer = new QsStmtAnalyzer(this);
        }

        QsExpAnalyzeResult AnalyzeExp(QsExp exp, QsAnalyzerContext context)
        {
            throw new NotImplementedException();
        }

        QsAnalyzeResult AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        {
            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가
            var resultBuilder = new QsAnalyzeResultBuilder();

            var declType = typeExpEvaluator.Evaluate(varDecl.Type);

            foreach (var elem in varDecl.Elements)
            {
                if (elem.InitExp == null)
                {
                    if (declType is QsVarTypeValue)
                        resultBuilder.AddError(elem, $"{elem.VarName}의 타입을 추론할 수 없습니다");
                    else 
                        context.AddVarType(elem.VarName, declType);
                }
                else
                {
                    var expResult = AnalyzeExp(elem.InitExp, context);
                    resultBuilder.AddResult(expResult.Result);

                    // var 처리
                    if (declType is QsVarTypeValue)
                        context.AddVarType(elem.VarName, expResult.Type);
                    else if (!IsAssignable(declType, expResult.Type))
                        resultBuilder.AddError(elem, $"타입 {expResult.Type}의 값은 타입 {declType.Name}의 변수 {elem.VarName}에 대입할 수 없습니다.");
                    else
                        context.AddVarType(elem.VarName, declType);
                }
            }

            return resultBuilder.ToResult();
        }

        QsAnalyzeResult AnalyzeScript(QsScript script, QsAnalyzerContext context)
        {
            // 1. type 모으기
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsEnumDeclScriptElement enumElem: break;
                }
            }

            // 2. func 시그니처를 모은다, type에 딸린 함수도 여기서 모은다 (1의 타입정보들이 필요하다)
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsFuncDeclScriptElement funcElem: break;
                }
            }

            // 3. stmt를 분석한다 (2의 함수정보가 필요하다)
            foreach (var elem in script.Elements)
            {
                switch(elem)
                {                    
                    case QsStmtScriptElement stmtElem: 
                }
            }

            // 4. 각 func body를 분석한다 (3에서 얻게되는 글로벌 변수 정보가 필요하다)
            foreach(var elem in script.Elements)
            {
                switch (elem)
                {
                    // TODO: classDecl
                    case QsFuncDeclScriptElement funcElem: break;
                }
            }
        }
    }
}
