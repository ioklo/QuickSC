using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsExpParserTest
    {
        async ValueTask<QsParserContext> MakeContextAsync(string input)        
        {
            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            return QsParserContext.Make(lexerContext);
        }

        [Fact]
        public async Task TestParseIdentifierExpAsync()
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync("x");
            var expResult = await expParser.ParseExpAsync(context);

            Assert.Equal(new QsIdentifierExp("x"), expResult.Elem);
        }

        [Fact]
        public async Task TestParseStringExpAsync()
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"");
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsStringExp(
                new QsTextStringExpElement("aaa bbb "),
                new QsExpStringExpElement(new QsStringExp(
                    new QsTextStringExpElement("xxx "),
                    new QsExpStringExpElement(new QsIdentifierExp("ddd")))),
                new QsTextStringExpElement(" ddd"));

            Assert.Equal(expected, expResult.Elem);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public async Task TestParseBoolAsync(string input, bool bExpectedResult)
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync(input);
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsBoolLiteralExp(bExpectedResult);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseIntAsync()
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync("1234");
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsIntLiteralExp(1234);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParsePrimaryExpAsync()
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync("(c % d)++");

            var expResult = await expParser.ParsePrimaryExpAsync(context);

            var expected = new QsUnaryOpExp(QsUnaryOpKind.PostfixInc,
                new QsBinaryOpExp(QsBinaryOpKind.Modulo,
                    new QsIdentifierExp("c"),
                    new QsIdentifierExp("d")));

            Assert.Equal(expected, expResult.Elem);
        }


        [Fact]
        public async Task TestParseComplexExpAsync()
        {
            var lexer = new QsLexer();
            var expParser = new QsExpParser(lexer);
            var context = await MakeContextAsync("a = b = !!(c % d)++ * e + f - g / h % i == 3 != false");
            
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsBinaryOpExp(QsBinaryOpKind.Assign,
                new QsIdentifierExp("a"),
                new QsBinaryOpExp(QsBinaryOpKind.Assign,
                    new QsIdentifierExp("b"),
                    new QsBinaryOpExp(QsBinaryOpKind.NotEqual,
                        new QsBinaryOpExp(QsBinaryOpKind.Equal,
                            new QsBinaryOpExp(QsBinaryOpKind.Subtract,
                                new QsBinaryOpExp(QsBinaryOpKind.Add,
                                    new QsBinaryOpExp(QsBinaryOpKind.Multiply,
                                        new QsUnaryOpExp(QsUnaryOpKind.LogicalNot,
                                            new QsUnaryOpExp(QsUnaryOpKind.LogicalNot,
                                                new QsUnaryOpExp(QsUnaryOpKind.PostfixInc,
                                                    new QsBinaryOpExp(QsBinaryOpKind.Modulo,
                                                        new QsIdentifierExp("c"),
                                                        new QsIdentifierExp("d"))))),
                                        new QsIdentifierExp("e")),
                                    new QsIdentifierExp("f")),
                                new QsBinaryOpExp(QsBinaryOpKind.Modulo,
                                    new QsBinaryOpExp(QsBinaryOpKind.Divide,
                                        new QsIdentifierExp("g"),
                                        new QsIdentifierExp("h")),
                                    new QsIdentifierExp("i"))),
                            new QsIntLiteralExp(3)),
                        new QsBoolLiteralExp(false))));

            Assert.Equal(expected, expResult.Elem);
        }
    }
}
