using QuickSC.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace QuickSC.EvalTest
{
    class QsTestCmdProvider : IQsCommandProvider
    {
        public bool Error = false;
        public string Output { get => sb.ToString(); }
        StringBuilder sb = new StringBuilder();

        public Task ExecuteAsync(string cmdText)
        {
            if (cmdText.StartsWith("echo "))
            {
                sb.Append(cmdText.Substring(5));
            }
            else
            {
                Error = true;
            }

            return Task.CompletedTask;
        }
    }

    class QsTestErrorCollector : IQsErrorCollector
    {
        List<(object, string)> messages = new List<(object, string)>();

        public bool HasError => messages.Count != 0;

        public void Add(object obj, string msg)
        {
            messages.Add((obj, msg));
        }
    }

    public class QsEvalTest
    {
        [Theory]
        [ClassData(typeof(QsEvalTestDataFactory))]
        public async Task TestEvaluateScript(QsEvalTestData data)
        {
            var cmdProvider = new QsTestCmdProvider();
            var app = new QsDefaultApplication(cmdProvider);

            string text;
            using(var reader = new StreamReader(data.Path))
            {
                text = reader.ReadToEnd();
            }

            Assert.StartsWith("// ", text);

            int firstLineEnd = text.IndexOfAny(new char[] { '\r', '\n' });
            Assert.True(firstLineEnd != -1);

            var expected = text.Substring(3, firstLineEnd - 3);

            var runtimeModule = new QsRuntimeModule();
            var errorCollector = new QsTestErrorCollector();
            await app.RunAsync(Path.GetFileNameWithoutExtension(data.Path), text, runtimeModule, ImmutableArray<IQsModule>.Empty, errorCollector);

            Assert.False(errorCollector.HasError);
            Assert.Equal(expected, cmdProvider.Output);
        }
    }
    
    public class QsEvalTestData : IXunitSerializable
    {
        public string Path { get; private set; }

        public QsEvalTestData()
        {
            Path = string.Empty;
        }

        public QsEvalTestData(string path)
        {
            Path = path;
        }

        public override string ToString()
        {
            return Path;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Path = info.GetValue<string>("Path");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("Path", Path);
        }
    }

    class QsEvalTestDataFactory : IEnumerable<object[]>
    {
        // List<QsEvalTestData> data;

        public QsEvalTestDataFactory()
        {
            //data = new List<QsEvalTestData>
            //{
            //    new QsEvalTestData(@"Input\Exp\IdentifierExp\01_GlobalVariable.qs", "1"),
            //    new QsEvalTestData(@"Input\Exp\IdentifierExp\02_GlobalScopedVariable.qs", "21"),
            //    new QsEvalTestData(@"Input\Exp\IdentifierExp\03_LocalVariable.qs", "21"),
            //    new QsEvalTestData(@"Input\Exp\StringExp\01_PlainText.qs", "hello"),
            //    new QsEvalTestData(@"Input\Exp\StringExp\02_Interpolation.qs", "hello.3"),
            //    new QsEvalTestData(@"Input\Exp\IntLiteral\01_SimpleInt.qs", "1024"),
            //    new QsEvalTestData(@"Input\Exp\BoolLiteral\01_SimpleBool.qs", "true false"),
            //    new QsEvalTestData(@"Input\Exp\BinaryOpExp\01_BoolOperation.qs", "false true true true false false true false true true false"),
            //    new QsEvalTestData(@"Input\Exp\BinaryOpExp\02_IntOperation.qs", "-3 4 4 false true true -4 -6 -26 2 3 true false true true false false true false true true"),
            //    new QsEvalTestData(@"Input\Exp\BinaryOpExp\03_StringOperation.qs", "hi hello world world true true false false onetwo true false true true false false true false true true"),
            //    new QsEvalTestData(@"Input\Exp\UnaryOpExp\01_BoolOperation.qs", "true false false true"),
            //    new QsEvalTestData(@"Input\Exp\UnaryOpExp\02_IntOperation.qs", "-3 3 -3 -2 -2 -3"),
            //    new QsEvalTestData(@"Input\Exp\CallExp\01_CallFunc.qs", "1 2 false"),
            //    new QsEvalTestData(@"Input\Exp\CallExp\02_CallLambda.qs", "1 3 true"),

            //};
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            var curDir = Directory.GetCurrentDirectory();

            foreach (var path in Directory.EnumerateFiles(curDir, "*.qs", SearchOption.AllDirectories))
            {
                var relPath = Path.GetRelativePath(curDir, path);
                yield return new object[] { new QsEvalTestData(relPath) };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
