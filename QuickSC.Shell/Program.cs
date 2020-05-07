using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Shell
{
    class Program
    {
        class QsDemoCommandProvider : IQsCommandProvider
        {
            StringBuilder sb = new StringBuilder();

            public async Task ExecuteAsync(string text)
            {
                try
                {
                    text = text.Trim();

                    if (text.StartsWith("echo "))
                    {
                        Console.WriteLine(text.Substring(5).Replace("\\n", "\n"));
                    }
                    else if (text.StartsWith("sleep "))
                    {
                        double f = double.Parse(text.Substring(6));

                        Console.WriteLine($"{f}초를 쉽니다");
                        await Task.Delay((int)(f * 1000));
                    }
                    else
                    {
                        Console.WriteLine($"알 수 없는 명령어 입니다: {text}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                // return Task.CompletedTask;
            }

            public string GetOutput() => sb.ToString();
        }

        static async Task Main(string[] args)
        {
            try
            {
                var lexer = new QsLexer();
                var parser = new QsParser(lexer);

                var cmdProvider = new QsDemoCommandProvider();
                var evaluator = new QsEvaluator(cmdProvider);
                var evalContext = QsEvalContext.Make();               
                var input = @"
enum MyEnum
{
    First,
    Second (int i)
}

var f = MyEnum.First;
var e = MyEnum.Second (2);

if (e is MyEnum.Second)
{
    @echo hi
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

                var newEvalContext = await evaluator.EvaluateScriptAsync(scriptResult.Elem, evalContext);
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

                    await foreach(var result in evaluator.EvaluateStmtAsync(stmtResult.Elem, evalContext))
                    {
                        if (!result.HasValue)
                            throw new Exception("실행에 실패했습니다");

                        evalContext = result.Value;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
