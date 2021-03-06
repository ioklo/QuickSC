using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Gum
{
    public class ExpParserTests
    {
        async ValueTask<(ExpParser, ParserContext)> PrepareAsync(string input)
        {
            var lexer = new Lexer();
            var parser = new Parser(lexer);
            var buffer = new Buffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = LexerContext.Make(bufferPos);
            var parserContext = ParserContext.Make(lexerContext);

            return (parser.expParser, parserContext);
        }

        [Fact]
        public async Task TestParseIdentifierExpAsync()
        {
            (var expParser, var context) = await PrepareAsync("x");

            var expResult = await expParser.ParseExpAsync(context);

            Assert.Equal(new IdentifierExp("x"), expResult.Elem);
        }

        [Fact]
        public async Task TestParseStringExpAsync()
        {
            var input = "\"aaa bbb ${\"xxx ${ddd}\"} ddd\"";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new StringExp(
                new TextStringExpElement("aaa bbb "),
                new ExpStringExpElement(new StringExp(
                    new TextStringExpElement("xxx "),
                    new ExpStringExpElement(new IdentifierExp("ddd")))),
                new TextStringExpElement(" ddd"));

            Assert.Equal(expected, expResult.Elem);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public async Task TestParseBoolAsync(string input, bool bExpectedResult)
        {   
            (var expParser, var context) = await PrepareAsync(input); 
            
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new BoolLiteralExp(bExpectedResult);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseIntAsync()
        {
            var input = "1234";

            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new IntLiteralExp(1234);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParsePrimaryExpAsync()
        {
            var input = "(c++(e, f) % d)++";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParsePrimaryExpAsync(context);

            var expected = new UnaryOpExp(UnaryOpKind.PostfixInc,
                new BinaryOpExp(BinaryOpKind.Modulo,
                    new CallExp(new UnaryOpExp(UnaryOpKind.PostfixInc, new IdentifierExp("c")), ImmutableArray<TypeExp>.Empty, new IdentifierExp("e"), new IdentifierExp("f")),
                    new IdentifierExp("d")));

            Assert.Equal(expected, expResult.Elem);
        }        

        [Fact]
        public async Task TestParseLambdaExpAsync()
        {
            var input = "a = b => (c, int d) => e";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new BinaryOpExp(BinaryOpKind.Assign,
                new IdentifierExp("a"),
                new LambdaExp(                    
                    new ReturnStmt(
                        new LambdaExp(
                            new ReturnStmt(new IdentifierExp("e")),
                            new LambdaExpParam(null, "c"),
                            new LambdaExpParam(new IdTypeExp("int"), "d"))),
                    new LambdaExpParam(null, "b")));

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseComplexMemberExpAsync()
        {
            var input = "a.b.c(1, \"str\").d";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected =
                new MemberExp(
                    new MemberCallExp(
                        new MemberExp(new IdentifierExp("a"), "b", ImmutableArray<TypeExp>.Empty),
                        "c",
                        ImmutableArray<TypeExp>.Empty,
                        new IntLiteralExp(1),
                        new StringExp(new TextStringExpElement("str"))),
                    "d",
                    ImmutableArray<TypeExp>.Empty);

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseListExpAsync()
        {
            var input = "[ 1, 2, 3 ]";
            (var expParser, var context) = await PrepareAsync(input);

            var expResult = await expParser.ParseExpAsync(context);

            var expected = new ListExp(
                null,
                new IntLiteralExp(1),
                new IntLiteralExp(2),
                new IntLiteralExp(3));
                
            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        public async Task TestParseComplexExpAsync()
        {
            var input = "a = b = !!(c % d)++ * e + f - g / h % i == 3 != false";
            (var expParser, var context) = await PrepareAsync(input);
            
            var expResult = await expParser.ParseExpAsync(context);

            var expected = new BinaryOpExp(BinaryOpKind.Assign,
                new IdentifierExp("a"),
                new BinaryOpExp(BinaryOpKind.Assign,
                    new IdentifierExp("b"),
                    new BinaryOpExp(BinaryOpKind.NotEqual,
                        new BinaryOpExp(BinaryOpKind.Equal,
                            new BinaryOpExp(BinaryOpKind.Subtract,
                                new BinaryOpExp(BinaryOpKind.Add,
                                    new BinaryOpExp(BinaryOpKind.Multiply,
                                        new UnaryOpExp(UnaryOpKind.LogicalNot,
                                            new UnaryOpExp(UnaryOpKind.LogicalNot,
                                                new UnaryOpExp(UnaryOpKind.PostfixInc,
                                                    new BinaryOpExp(BinaryOpKind.Modulo,
                                                        new IdentifierExp("c"),
                                                        new IdentifierExp("d"))))),
                                        new IdentifierExp("e")),
                                    new IdentifierExp("f")),
                                new BinaryOpExp(BinaryOpKind.Modulo,
                                    new BinaryOpExp(BinaryOpKind.Divide,
                                        new IdentifierExp("g"),
                                        new IdentifierExp("h")),
                                    new IdentifierExp("i"))),
                            new IntLiteralExp(3)),
                        new BoolLiteralExp(false))));

            Assert.Equal(expected, expResult.Elem);
        }
    }
}
