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
            sb.Append(cmdText);

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
