using QuickSC.StaticAnalyzer;
using QuickSC.TypeExpEvaluator;
using System;
using System.Collections.Immutable;
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

                // var typeValueFactory = new QsTypeValueFactory();
                var analyzer = new QsAnalyzer();
                var analyzerContext = new QsAnalyzerContext();                

                var evaluator = new QsEvaluator(cmdProvider);
                
                var input = @"

int a = 0;

enum X 
{
    First,
    Second
}

void Func(X x)
{
    if (x is First)
        @echo hi
}

Func(First);

@echo $a

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

                analyzer.AnalyzeScript(scriptResult.Elem, analyzerContext);

                var evalStaticContext = new QsEvalStaticContext(analyzerContext.TypeExpTypeValues.ToImmutableDictionary());
                var evalContext = QsEvalContext.Make(evalStaticContext);
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
    }
}
