using QuickSC.Token;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsNormalLexerTest
    {
        QsBufferPosition ToPosition(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return buffer.MakePosition();
        }

        [Fact]
        public async ValueTask TestLexSimpleIdentifier()
        {   
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("x"));

            Assert.True(token.HasValue);
            Assert.Equal(new QsIdentifierToken("x"), token?.Token);
        }

        [Fact]
        public async ValueTask TestLexAlternativeIdentifier()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("@for"));
            
            Assert.Equal(new QsIdentifierToken("for"), token?.Token);
        }

        [Fact]
        public async ValueTask TestLexNormalString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("  \"aaa bbb \"  "));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),                
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexDoubleQuoteString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("\" \"\" \"  "));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement(" \" "),
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexDollarString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("\"$$\""));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("$"),
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexSimpleEscapedString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("\"aaa bbb $ccc ddd\""));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),
                new QsTextStringTokenElement(" ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexEscapedString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("\"aaa bbb ${ccc} ddd\""));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),
                new QsTextStringTokenElement(" ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public async ValueTask TestLexComplexString()
        {
            var lexer = new QsNormalLexer();
            var token = await lexer.GetNextTokenAsync(ToPosition("\"aaa bbb ${\"xxx ${ddd}\"} ddd\""));

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),
                new QsTokenStringTokenElement(new QsStringToken(new List<QsStringTokenElement>
                {
                    new QsTextStringTokenElement("xxx "),
                    new QsTokenStringTokenElement(new QsIdentifierToken("ddd"))
                })),
                new QsTextStringTokenElement(" ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }
    }
}
