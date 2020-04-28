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
            try
            {
                var lexer = new QsLexer();
                var parser = new QsParser(lexer);

                var cmdProvider = new QsCmdCommandProvider();
                var evaluator = new QsEvaluator(cmdProvider);
                var evalContext = QsEvalContext.Make();               
                var input = @"
int jobCount = 0; // no guard for contention

task 
{
    jobCount++;
    @timeout 4 /nobreak > nul 
    @echo Completed 1!
    jobCount--;
}

task 
{
    jobCount++;
    @timeout 2 /nobreak > nul 
    @echo Completed 2!
    jobCount--;
}

for(int i = 0 ; i < 6; i++)
@{
    timeout 1 /nobreak > nul 
    echo ${i+1} sec $jobCount jobs left
}
";
                var buffer = new QsBuffer(new StringReader(input));
                var pos = await buffer.MakePosition().NextAsync();
                var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));

                var scriptResult = await parser.ParseScriptAsync(parserContext);
                if (!scriptResult.HasValue)
                {
                    Console.WriteLine("파싱에 실패했습니다");
                    return;
                }

                var newEvalContext = evaluator.EvaluateScript(scriptResult.Elem, evalContext);
                if (!newEvalContext.HasValue)
                {
                    Console.WriteLine("실행에 실패했습니다");
                    return;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static async Task Main2(string[] args)
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);

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
                    var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));

                    sb.Clear();

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
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
