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
using QuickSC.StaticAnalyzer;
using QuickSC.Runtime;
using System.Linq;

namespace QuickSC.Blazor
{
    public class Program
    {
        internal static IJSRuntime? jsRuntime;

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

        class QsDemoErrorCollector : IQsErrorCollector
        {
            public List<(object, string)> Messages = new List<(object, string)>();

            public bool HasError => Messages.Count != 0;

            public void Add(object obj, string msg)
            {
                Messages.Add((obj, msg));
            }
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
                var demoCmdProvider = new QsDemoCommandProvider();
                var app = new QsDefaultApplication(demoCmdProvider);
                var runtimeModule = new QsRuntimeModule();
                var errorCollector = new QsDemoErrorCollector();

                var runResult = await app.RunAsync("Demo", input, runtimeModule, ImmutableArray<IQsModule>.Empty, errorCollector);
                
                if (errorCollector.HasError)
                {
                    foreach(var (obj, msg) in errorCollector.Messages)
                    {
                        await WriteAsync(string.Join("\n", errorCollector.Messages.Select(m => m.Item2)));
                    }

                    return false;
                }

                if (!runResult)
                    await WriteAsync("실행 실패");

                return runResult;
            }
            catch (Exception e)
            {
                await WriteAsync(e.ToString());
                return false;
            }
        }
    }
}
