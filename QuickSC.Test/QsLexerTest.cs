using QuickSC.Token;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace QuickSC
{
    public class QsLexerTest
    {
        [Fact]
        public void TestLexSimpleIdentifier()
        {
            var lexer = new QsLexer("x");
            var token = lexer.LexToken("x", 0);

            Assert.True(token.HasValue);
            Assert.Equal(new QsIdentifierToken("x"), token?.Token);
        }

        [Fact]
        public void TestLexAlternativeIdentifier()
        {
            var lexer = new QsLexer("@for");
            var token = lexer.LexToken("@for", 0);
            
            Assert.Equal(new QsIdentifierToken("for"), token?.Token);
        }

        [Fact]
        public void TestLexNormalString()
        {
            var lexer = new QsLexer("  \"aaa bbb \"  ");
            var token = lexer.LexToken("  \"aaa bbb \"  ", 0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),                
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public void TestLexDoubleQuoteString()
        {
            var lexer = new QsLexer("\" \"\" \"  ");
            var token = lexer.LexToken("\" \"\" \"  ", 0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement(" \" "),
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public void TestLexDollarString()
        {
            var lexer = new QsLexer("\"$$\"");
            var token = lexer.LexToken("\"$$\"", 0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("$"),
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public void TestLexSimpleEscapedString()
        {
            var lexer = new QsLexer("\"aaa bbb $ccc ddd\"");
            var token = lexer.LexToken("\"aaa bbb $ccc ddd\"", 0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),
                new QsTextStringTokenElement(" ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public void TestLexEscapedString()
        {
            var lexer = new QsLexer("\"aaa bbb ${ccc} ddd\"");
            var token = lexer.LexToken("\"aaa bbb ${ccc} ddd\"", 0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("aaa bbb "),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),
                new QsTextStringTokenElement(" ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }

        [Fact]
        public void TestLexComplexString()
        {
            var lexer = new QsLexer("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"");
            var token = lexer.LexToken("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"", 0);

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

        [Fact]
        public void TestLexCommandTokenReturnIdentifier()
        {
            var lexer = new QsLexer("abcd");
            var token = lexer.LexCommandToken(0);

            var expectedToken = new QsIdentifierToken("abcd");
            Assert.Equal(expectedToken, token?.Token);        
        }

        [Fact]
        public void TestLexCommandTokenReturnString()
        {
            var lexer = new QsLexer("ps${ccc}ddd");
            var token = lexer.LexCommandToken(0);

            var expectedToken = new QsStringToken(new List<QsStringTokenElement>
            {
                new QsTextStringTokenElement("ps"),
                new QsTokenStringTokenElement(new QsIdentifierToken("ccc")),                
                new QsTextStringTokenElement("ddd")
            });

            Assert.Equal(expectedToken, token?.Token);
        }
    }
}
