using QuickSC.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

        public async Task ExecuteAsync(string cmdText)
        {
            if (cmdText == "yield")
            {
                // TODO: 좋은 방법이 있으면 교체한다
                await Task.Delay(500);
                return;
            }

            sb.Append(cmdText);
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

        public string GetMessages()
        {
            return string.Join("\r\n", messages.Select(message => message.Item2));
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

            var runtimeModuleInfo = new QsRuntimeModuleInfo();
            var errorCollector = new QsTestErrorCollector();
            await app.RunAsync(Path.GetFileNameWithoutExtension(data.Path), text, runtimeModuleInfo, ImmutableArray<IQsModule>.Empty, errorCollector);

            Assert.False(errorCollector.HasError, errorCollector.GetMessages());
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
