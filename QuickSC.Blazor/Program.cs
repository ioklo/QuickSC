using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.IO;
using System.Collections.Immutable;
using System.Threading;

namespace QuickSC.Blazor
{
    public class Program
    {
        static IJSRuntime? jsRuntime;

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddBaseAddressHttpClient();

            var host = builder.Build();
            jsRuntime = host.Services.GetService<IJSRuntime>();

            await host.RunAsync();
        }

        static async Task WriteAsync(string msg)
        {
            await jsRuntime.InvokeVoidAsync("writeConsole", msg);
        }

        class QsDemoCommandProvider : IQsCommandProvider
        {
            public QsDemoCommandProvider()
            {   
            }            

            public async Task ExecuteAsync(string text)
            {   
                try
                {
                    text = text.Trim();

                    if (text.StartsWith("echo "))
                    {
                        await WriteAsync(text.Substring(5).Replace("\\n", "\n"));
                    }
                    else if (text.StartsWith("sleep "))
                    {
                        var d = double.Parse(text.Substring(6));
                        await Task.Delay((int)(1000 * d));
                    }
                    else
                    {
                        await WriteAsync($"알 수 없는 명령어 입니다: {text}\n");
                    }
                }
                catch (Exception e)
                {
                    await WriteAsync(e.ToString() + "\n");
                }

                // return Task.CompletedTask;
            }
        }

        [JSInvokable]
        public static async Task<bool> RunAsync(string input)
        {
            try
            {
                var lexer = new QsLexer();
                var parser = new QsParser(lexer);
                var buffer = new QsBuffer(new StringReader(input));
                var pos = await buffer.MakePosition().NextAsync();
                var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));

                var scriptResult = await parser.ParseScriptAsync(parserContext);
                if (!scriptResult.HasValue)
                {
                    await WriteAsync("에러 (파싱 실패)");
                    return false;
                }

                var demoCmdProvider = new QsDemoCommandProvider();

                var evaluator = new QsEvaluator(demoCmdProvider);
                var evalContext = QsEvalContext.Make();
                var newEvalContext = await evaluator.EvaluateScriptAsync(scriptResult.Elem, evalContext);
                if (newEvalContext == null)
                {
                    await WriteAsync("에러 (실행 실패)");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                await WriteAsync(e.ToString());
                return false;
            }
        }
    }
}
