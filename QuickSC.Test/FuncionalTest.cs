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

            // �ؾ� �� ��
            // Abstract Syntax �����
            // 1. ����
            // 2. $, ${ .... }
            // 3. Literal 
            // Script = ExecutionExpression

            // ��ü �׽�Ʈ
            // literal�� ������ ����..
            // 

            // �Ľ� �׽�Ʈ
            // SyntaxTree Execution �׽�Ʈ
        }
    }
}
