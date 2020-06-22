using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickSC.Shell
{
    class Program
    {
        class QsDemoErrorCollector : IQsErrorCollector
        {
            public List<(object, string)> Messages = new List<(object, string)>();

            public bool HasError => Messages.Count != 0;

            public void Add(object obj, string msg)
            {
                Messages.Add((obj, msg));
            }
        }

        class QsRawCommandProvider : IQsCommandProvider
        {
            public Task ExecuteAsync(string cmdText)
            {
                var tcs = new TaskCompletionSource<int>();

                var match = Regex.Match(cmdText, @"^\s*([^\s]+)\s*(.*)$");
                if (!match.Success) return Task.CompletedTask;

                var fileName = match.Groups[1].Value;
                var arguments = match.Groups[2].Value;

                var psi = new ProcessStartInfo(fileName, arguments);
                psi.UseShellExecute = false;

                var process = new Process();
                process.StartInfo = psi;
                process.EnableRaisingEvents = true;

                process.Exited += (sender, args) =>
                {
                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                };

                process.Start();

                return tcs.Task;
            }
        }
            

        class QsCmdCommandProvider : IQsCommandProvider
        {   
            public Task ExecuteAsync(string cmdText)
            {
                var tcs = new TaskCompletionSource<int>();

                var psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + cmdText;
                psi.UseShellExecute = false;

                var process = new Process();
                process.StartInfo = psi;
                process.EnableRaisingEvents = true;

                process.Exited += (sender, args) =>
                {
                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                };

                process.Start();

                return tcs.Task;
            }
        }


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

        public static IQsCommandProvider? MakeCommandProvider()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var shellEnv = Environment.GetEnvironmentVariable("SHELL");
                if (shellEnv != null)
                    return new QsRawCommandProvider();

                return null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // cmd를 사용한다.
                return new QsCmdCommandProvider();
            }

            return null;
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: QuickSC.Shell [script file]");
                return;
            }

            try
            {
                var cmdProvider = MakeCommandProvider();
                if (cmdProvider == null)
                {
                    Console.WriteLine("Failed to choose appropriate command provider");
                    return;
                }

                // code
                var app = new QsDefaultApplication(cmdProvider);                
                var errorCollector = new QsDemoErrorCollector();

                using (var stream = new StreamReader(args[0]))
                {
                    var fullPath = Path.GetFullPath(args[0]);
                    var scriptDir = Path.GetDirectoryName(fullPath);
                    if (scriptDir == null) return;

                    var runtimeModule = new QsRuntimeModule(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), scriptDir);

                    var input = await stream.ReadToEndAsync();
                    var moduleName = Path.GetFileNameWithoutExtension(fullPath);
                    var runResult = await app.RunAsync(moduleName, input, runtimeModule, ImmutableArray<IQsModule>.Empty, errorCollector);
                    
                    if (errorCollector.HasError)
                    {
                        foreach(var (obj, msg) in errorCollector.Messages)
                        {
                            Console.WriteLine($"{obj}: {msg}");
                        }
                    }
                    else if (runResult == null)
                    {
                        Console.WriteLine("실행 에러");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
