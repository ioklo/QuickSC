using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class FuncionalTest
    {
        [Fact]
        public async Task TestEvaluateScript()
        {
            var evaluator = new QsEvaluator();
            var buffer = new QsBuffer(new StringReader("cmd /c notepad"));
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var parserContext = QsParserContext.Make(QsLexerContext.Make(await buffer.MakePosition().NextAsync()));

            var scriptResult = await parser.ParseScriptAsync(parserContext);

            var evalContext = new QsEvalContext();
            evaluator.EvaluateScript(scriptResult.Elem, evalContext);

            // 해야 할 일
            // Abstract Syntax 만들기
            // 1. 실행
            // 2. $, ${ .... }
            // 3. Literal 
            // Script = ExecutionExpression

            // 전체 테스트
            // literal을 쓸일이 없다..
            // 

            // 파싱 테스트
            // SyntaxTree Execution 테스트
        }
    }
}
