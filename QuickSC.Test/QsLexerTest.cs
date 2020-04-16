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

        async ValueTask<IEnumerable<QsToken>> ProcessInnerAsync(Func<QsLexerContext, ValueTask<QsLexResult>> lexAction, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while (true)
            {
                var lexResult = await lexAction(context);
                if (!lexResult.HasValue || lexResult.Token is QsEndOfFileToken) break;

                context = lexResult.Context;

                result.Add(lexResult.Token);
            }

            return result;
        }

        ValueTask<IEnumerable<QsToken>> ProcessNormalAsync(QsLexer lexer, QsLexerContext context)
        {
            return ProcessInnerAsync(context => lexer.LexNormalModeAsync(context), context);
        }

        ValueTask<IEnumerable<QsToken>> ProcessStringAsync(QsLexer lexer, QsLexerContext context)
        {
            return ProcessInnerAsync(context => lexer.LexStringModeAsync(context), context);
        }

        [Fact]
        public async Task TestLexSymbols()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync(", ; =");

            var tokens = await ProcessNormalAsync(lexer, context);
            var expectedTokens = new QsToken[]
            {
                new QsCommaToken(),
                new QsSemiColonToken(),
                new QsEqualToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexKeywords()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("true false");

            var tokens = await ProcessNormalAsync(lexer, context);
            var expectedTokens = new QsToken[]
            {
                new QsBoolToken(true),
                new QsBoolToken(false),                
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexSimpleIdentifier()
        {   
            var lexer = new QsLexer();
            var token = await lexer.LexNormalModeAsync(await MakeContextAsync("x"));

            Assert.True(token.HasValue);
            Assert.Equal(new QsIdentifierToken("x"), token.Token);
        }

        [Fact]
        public async Task TestLexAlternativeIdentifier()
        {
            var lexer = new QsLexer();
            var token = await lexer.LexNormalModeAsync(await MakeContextAsync("@for"));
            
            Assert.Equal(new QsIdentifierToken("for"), token.Token);
        }

        [Fact]
        public async Task TestLexNormalString()
        {
            var context = await MakeContextAsync("  \"aaa bbb \"  ");
            var lexer = new QsLexer();
            var result0 = await lexer.LexNormalModeAsync(context);
            var result1 = await lexer.LexStringModeAsync(result0.Context);
            var result2 = await lexer.LexStringModeAsync(result1.Context);

            Assert.Equal(new QsDoubleQuoteToken(), result0.Token);
            Assert.Equal(new QsTextToken("aaa bbb "), result1.Token);
            Assert.Equal(new QsDoubleQuoteToken(), result2.Token);
        }

        // stringMode
        [Fact]
        public async Task TestLexDoubleQuoteString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"\"");

            var tokenResult = await lexer.LexStringModeAsync(context);

            var expectedToken = new QsTextToken("\"");

            Assert.Equal(expectedToken, tokenResult.Token);
        }

        [Fact]
        public async Task TestLexDollarString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("$$");

            var tokenResult = await lexer.LexStringModeAsync(context);
            var expectedToken = new QsTextToken("$");
            Assert.Equal(expectedToken, tokenResult.Token);
        }

        [Fact]
        public async Task TestLexSimpleEscapedString2()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("$ccc");

            var tokenResult = await lexer.LexStringModeAsync(context);
            var expectedToken = new QsIdentifierToken("ccc");
            Assert.Equal(expectedToken, tokenResult.Token);
        }

        [Fact]
        public async Task TestLexSimpleEscapedString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("aaa bbb $ccc ddd");

            var tokens = await ProcessStringAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("aaa bbb "),
                new QsIdentifierToken("ccc"),
                new QsTextToken(" ddd"),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexEscapedString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("aaa bbb ${ccc} ddd");

            var tokens = new List<QsToken>();
            var result = await lexer.LexStringModeAsync(context);
            tokens.Add(result.Token);

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("aaa bbb "),
                new QsDollarLBraceToken(),
                new QsIdentifierToken("ccc"),
                new QsRBraceToken(),
                new QsTextToken(" ddd"),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexComplexString()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"");

            var tokens = new List<QsToken>();
            var result = await lexer.LexNormalModeAsync(context);
            tokens.Add(result.Token); // "

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // aaa bbb

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ${

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // "

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // xxx 

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ${

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // ddd

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // }

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // "

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // }

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ddd 

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // "

            var expectedTokens = new QsToken[]
            {
                new QsDoubleQuoteToken(),
                new QsTextToken("aaa bbb "),
                new QsDollarLBraceToken(),
                
                new QsDoubleQuoteToken(),

                new QsTextToken("xxx "),
                new QsDollarLBraceToken(),
                new QsIdentifierToken("ddd"),
                new QsRBraceToken(),
                new QsDoubleQuoteToken(),
                new QsRBraceToken(),
                new QsTextToken(" ddd"),
                new QsDoubleQuoteToken(),
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexInt()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("1234"); // 나머지는 지원 안함

            var result = await lexer.LexNormalModeAsync(context);
            var expectedToken = new QsIntToken(1234);

            Assert.Equal(expectedToken, result.Token);
        }
    }
}
