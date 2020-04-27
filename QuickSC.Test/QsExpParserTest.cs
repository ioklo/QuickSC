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
        async ValueTask<(QsExpParser, QsParserContext)> PrepareAsync(string input)
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            var parserContext = QsParserContext.Make(lexerContext);

            return (parser.expParser, parserContext);
        }

        [Fact]
        public async Task TestParseIdentifierExpAsync()
        {
            (var expParser, var context) = await PrepareAsync("x");

            var expResult = await expParser.ParseExpAsync(context);

            Assert.Equal(new QsIdentifierExp("x"), expResult.Elem);
        }

        [Fact]
        public async Task TestParseStringExpAsync()
        {
            var input = "\"aaa bbb ${\"xxx ${ddd}\"} ddd\"";
            (var expParser, var context) = await PrepareAsync(input);

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
            (var expParser, var context) = await PrepareAsync(input); 
            
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsBoolLiteralExp(bExpectedResult);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseIntAsync()
        {
            var input = "1234";

            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsIntLiteralExp(1234);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParsePrimaryExpAsync()
        {
            var input = "(c++(e, f) % d)++";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParsePrimaryExpAsync(context);

            var expected = new QsUnaryOpExp(QsUnaryOpKind.PostfixInc,
                new QsBinaryOpExp(QsBinaryOpKind.Modulo,
                    new QsCallExp(new QsExpCallExpCallable(new QsUnaryOpExp(QsUnaryOpKind.PostfixInc, new QsIdentifierExp("c"))), new QsIdentifierExp("e"), new QsIdentifierExp("f")),
                    new QsIdentifierExp("d")));

            Assert.Equal(expected, expResult.Elem);
        }        

        [Fact]
        public async Task TestParseLambdaExpAsync()
        {
            var input = "a = b => (c, int d) => e";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new QsBinaryOpExp(QsBinaryOpKind.Assign,
                new QsIdentifierExp("a"),
                new QsLambdaExp(
                    new QsReturnStmt(
                        new QsLambdaExp(
                            new QsReturnStmt(new QsIdentifierExp("e")),
                            new QsLambdaExpParam(null, "c"),
                            new QsLambdaExpParam(new QsTypeIdExp("int"), "d"))),
                    new QsLambdaExpParam(null, "b")));

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseComplexExpAsync()
        {
            var input = "a = b = !!(c % d)++ * e + f - g / h % i == 3 != false";
            (var expParser, var context) = await PrepareAsync(input);
            
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
