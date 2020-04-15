using QuickSC.Token;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsLexerTest
    {
        async ValueTask<QsLexerContext> MakeContextAsync(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return QsLexerContext.Make(await buffer.MakePosition().NextAsync()); // TODO: Position 관련 동작 재 수정
        }

        async Task<IEnumerable<QsToken>> ProcessAsync(QsLexer lexer, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while (true)
            {
                var lexResult = await lexer.LexAsync(context);
                if (!lexResult.HasValue) break;

                context = lexResult.Context;

                result.Add(lexResult.Token);
            }

            return result;
        }

        [Fact]
        public async Task TestLexSymbols()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync(", ; =");

            var tokens = await ProcessAsync(lexer, context);
            var expectedTokens = new QsToken[]
            {
                new QsCommaToken(),
                new QsSemiColonToken(),
                new QsEqualToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);

        }

        [Fact]
        public async Task TestLexSimpleIdentifier()
        {   
            var lexer = new QsLexer();
            var token = await lexer.LexAsync(await MakeContextAsync("x"));

            Assert.True(token.HasValue);
            Assert.Equal(new QsIdentifierToken("x"), token.Token);
        }

        [Fact]
        public async Task TestLexAlternativeIdentifier()
        {
            var lexer = new QsLexer();
            var token = await lexer.LexAsync(await MakeContextAsync("@for"));
            
            Assert.Equal(new QsIdentifierToken("for"), token.Token);
        }

        [Fact]
        public async Task TestLexNormalString()
        {
            var context = await MakeContextAsync("  \"aaa bbb \"  ");
            var lexer = new QsLexer();
            var result0 = await lexer.LexAsync(context);
            var result1 = await lexer.LexAsync(result0.Context);
            var result2 = await lexer.LexAsync(result1.Context);

            Assert.Equal(new QsBeginStringToken(), result0.Token);
            Assert.Equal(new QsTextToken("aaa bbb "), result1.Token);
            Assert.Equal(new QsEndStringToken(), result2.Token);
        }

        [Fact]
        public async Task TestLexDoubleQuoteString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\" \"\" \"  ");

            var tokens = await ProcessAsync(lexer, context);
            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsTextToken(" \" "),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexDollarString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"$$\"");

            var tokens = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsTextToken("$"),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexSimpleEscapedString2()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"$ccc\"");

            var tokens = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsIdentifierToken("ccc"),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexSimpleEscapedString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"aaa bbb $ccc ddd\"");

            var tokens = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsTextToken("aaa bbb "),
                new QsIdentifierToken("ccc"),
                new QsTextToken(" ddd"),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexEscapedString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"aaa bbb ${ccc} ddd\"");

            var tokens = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsTextToken("aaa bbb "),
                new QsBeginInnerExpToken(),
                new QsIdentifierToken("ccc"),
                new QsEndInnerExpToken(),
                new QsTextToken(" ddd"),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexComplexString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"");

            var tokens = await ProcessAsync(lexer, context);


            var expectedTokens = new QsToken[]
            {
                new QsBeginStringToken(),
                new QsTextToken("aaa bbb "),
                new QsBeginInnerExpToken(),
                
                new QsBeginStringToken(),

                new QsTextToken("xxx "),
                new QsBeginInnerExpToken(),
                new QsIdentifierToken("ddd"),
                new QsEndInnerExpToken(),
                new QsEndStringToken(),
                new QsEndInnerExpToken(),
                new QsTextToken(" ddd"),
                new QsEndStringToken(),
                new QsEndOfFileToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }
    }
}
