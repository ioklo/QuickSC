using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsParserTest
    {
        [Fact]
        public async ValueTask TestSimpleCaseAsync()
        {
            var parser = new QsParser();
            var script = await parser.ParseScriptAsync(new QsBuffer(new StringReader("ls -al")).MakePosition());

        }
    }
}
