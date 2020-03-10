using System;
using Xunit;

namespace QuickSC
{
    public class FuncionalTest
    {
        [Fact]
        public void TestExecution()
        {
            var evaluator = new QsEvaluator();
            evaluator.Evaluate("dir");

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
