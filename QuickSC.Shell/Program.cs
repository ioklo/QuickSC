using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Shell
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var lexer = new QsLexer();
            var stmtParser = new QsStmtParser(lexer);

            var cmdProvider = new QsCmdCommandProvider();
            var evaluator = new QsEvaluator(cmdProvider);
            var evalContext = QsEvalContext.Make();

            var sb = new StringBuilder();

            // Statement만 입력으로 받고
            while (true)
            {
                try
                {
                    if (sb.Length == 0)
                    {
                        Console.WriteLine();
                        Console.Write("QS {0}>", Directory.GetCurrentDirectory());
                    }
                    else
                    {
                        Console.Write(">");
                    }

                    var line = Console.ReadLine();

                    if (line.EndsWith('\\'))
                    {                        
                        sb.AppendLine(line.Substring(0, line.Length - 1));
                        continue;
                    }
                    else
                    {
                        sb.Append(line);
                    }

                    var buffer = new QsBuffer(new StringReader(sb.ToString()));
                    var pos = await buffer.MakePosition().NextAsync();
                    var parserContext = QsParserContext.Make(QsLexerContext.Make(pos)).AddType("int").AddType("string");

                    sb.Clear();

                    var stmtResult = await stmtParser.ParseStmtAsync(parserContext);
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
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
