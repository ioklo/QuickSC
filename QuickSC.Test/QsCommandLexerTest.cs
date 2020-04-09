using QuickSC.Token;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{   
    public class QsCommandLexerTest
    {
        QsBufferPosition ToPosition(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return buffer.MakePosition();
        }

        [Fact]
        public async ValueTask TestNextCommandTokenReturnIdentifier()
        {
            var normalLexer = new QsNormalLexer();
            var lexer = new QsCommandLexer(normalLexer);
            var token = await lexer.GetNextCommandTokenAsync(ToPosition("abcd"));

            var expectedToken = new QsIdentifierCommandToken(new QsIdentifierToken("abcd"));
            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexCommandTokenReturnString()
        {
            var normalLexer = new QsNormalLexer();
            var lexer = new QsCommandLexer(normalLexer);
            var token = await lexer.GetNextCommandTokenAsync(ToPosition("ps${ccc}ddd"));

            var expectedToken = new QsStringCommandToken(new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("ps"),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),
                new QsTextStringTokenElement("ddd")
            }));

            Assert.Equal(expectedToken, token?.Token);
        }
    }
}
