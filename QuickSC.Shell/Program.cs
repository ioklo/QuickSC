using System;
using System.IO;
using System.Threading.Tasks;

namespace QuickSC.Shell
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);

            var evaluator = new QsEvaluator();
            var evalContext = QsEvalContext.Make();

            // Statement만 입력으로 받고
            while (true)
            {
                try
                {
                    Console.WriteLine();
                    Console.Write("QS {0}>", Directory.GetCurrentDirectory());

                    var line = Console.ReadLine();
                    var buffer = new QsBuffer(new StringReader(line));
                    var pos = await buffer.MakePosition().NextAsync();
                    var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));

                    var stmtResult = await parser.ParseStmtAsync(parserContext);
                    if (!stmtResult.HasValue)
                    {
                        Console.WriteLine("파싱에 실패했습니다");
                        continue;
                    }

                    var newEvalContext = evaluator.EvaluateStmt(stmtResult.Elem, evalContext);
                    if (!newEvalContext.HasValue)
                    {
                        Console.WriteLine("실행에 실패했습니다");
                        continue;
                    }

                    evalContext = newEvalContext.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
