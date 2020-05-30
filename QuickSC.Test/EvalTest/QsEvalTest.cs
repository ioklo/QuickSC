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

            var runtimeModule = new QsRuntimeModule();
            var errorCollector = new QsTestErrorCollector();
            await app.RunAsync(Path.GetFileNameWithoutExtension(data.Path), text, runtimeModule, ImmutableArray<IQsModule>.Empty, errorCollector);

            Assert.False(errorCollector.HasError);
            Assert.Equal(data.Expected, cmdProvider.Output);
        }
    }
    
    public class QsEvalTestData : IXunitSerializable
    {
        public string Path { get; private set; }
        public string Expected { get; private set; }

        public QsEvalTestData()
        {
            Path = string.Empty;
            Expected = string.Empty;
        }

        public QsEvalTestData(string path, string expected)
        {
            Path = path;
            Expected = expected;
        }

        public override string ToString()
        {
            return Path;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Path = info.GetValue<string>("Path");
            Expected = info.GetValue<string>("Expected");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("Path", Path);
            info.AddValue("Expected", Expected);
        }
    }

    class QsEvalTestDataFactory : IEnumerable<object[]>
    {
        List<QsEvalTestData> data;

        public QsEvalTestDataFactory()
        {
            data = new List<QsEvalTestData>
            {
                new QsEvalTestData(@"Input\Exp\IdentifierExp\01_GlobalVariable.qs", "1"),
                new QsEvalTestData(@"Input\Exp\IdentifierExp\02_GlobalScopedVariable.qs", "21"),
                new QsEvalTestData(@"Input\Exp\IdentifierExp\03_LocalVariable.qs", "21"),
            };
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var item in data)
                yield return new object[] { item };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
