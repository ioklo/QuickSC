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
            return ProcessInnerAsync(context => lexer.LexNormalModeAsync(context, false), context);
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
                QsCommaToken.Instance,
                QsSemiColonToken.Instance,
                QsEqualToken.Instance,
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
            var token = await lexer.LexNormalModeAsync(await MakeContextAsync("x"), false);

            Assert.True(token.HasValue);
            Assert.Equal(new QsIdentifierToken("x"), token.Token);
        }

        [Fact]
        public async Task TestLexNormalString()
        {
            var context = await MakeContextAsync("  \"aaa bbb \"  ");
            var lexer = new QsLexer();
            var result0 = await lexer.LexNormalModeAsync(context, false);
            var result1 = await lexer.LexStringModeAsync(result0.Context);
            var result2 = await lexer.LexStringModeAsync(result1.Context);

            Assert.Equal(QsDoubleQuoteToken.Instance, result0.Token);
            Assert.Equal(new QsTextToken("aaa bbb "), result1.Token);
            Assert.Equal(QsDoubleQuoteToken.Instance, result2.Token);
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
            var context = await MakeContextAsync("aaa bbb ${ccc} ddd"); // TODO: "aaa bbb ${ ccc \r\n } ddd" 는 에러

            var tokens = new List<QsToken>();
            var result = await lexer.LexStringModeAsync(context);
            tokens.Add(result.Token);

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token);

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token);

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("aaa bbb "),
                QsDollarLBraceToken.Instance,
                new QsIdentifierToken("ccc"),
                QsRBraceToken.Instance,
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
            var result = await lexer.LexNormalModeAsync(context, false);
            tokens.Add(result.Token); // "

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // aaa bbb

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ${

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token); // "

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // xxx 

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ${

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token); // ddd

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token); // }

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // "

            result = await lexer.LexNormalModeAsync(result.Context, false);
            tokens.Add(result.Token); // }

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // ddd 

            result = await lexer.LexStringModeAsync(result.Context);
            tokens.Add(result.Token); // "

            var expectedTokens = new QsToken[]
            {
                QsDoubleQuoteToken.Instance,
                new QsTextToken("aaa bbb "),
                QsDollarLBraceToken.Instance,

                QsDoubleQuoteToken.Instance,

                new QsTextToken("xxx "),
                QsDollarLBraceToken.Instance,
                new QsIdentifierToken("ddd"),
                QsRBraceToken.Instance,
                QsDoubleQuoteToken.Instance,
                QsRBraceToken.Instance,
                new QsTextToken(" ddd"),
                QsDoubleQuoteToken.Instance,
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexInt()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("1234"); // 나머지는 지원 안함

            var result = await lexer.LexNormalModeAsync(context, false);
            var expectedToken = new QsIntToken(1234);

            Assert.Equal(expectedToken, result.Token);
        }

        [Fact]
        public async Task TestLexComment()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("  // e s \r\n// \r// \n1234"); // 나머지는 지원 안함

            var tokens = new List<QsToken>();

            var result = await lexer.LexWhitespaceAsync(context, false);
            tokens.Add(result.Token);

            result = await lexer.LexNewLineAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexWhitespaceAsync(result.Context, false);
            tokens.Add(result.Token);

            result = await lexer.LexNewLineAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexWhitespaceAsync(result.Context, false);
            tokens.Add(result.Token);

            result = await lexer.LexNewLineAsync(result.Context);
            tokens.Add(result.Token);

            result = await lexer.LexIntAsync(result.Context);
            tokens.Add(result.Token);

            var expectedTokens = new QsToken[] {
                QsWhitespaceToken.Instance,
                QsNewLineToken.Instance,
                QsWhitespaceToken.Instance,
                QsNewLineToken.Instance,
                QsWhitespaceToken.Instance,
                QsNewLineToken.Instance,
                new QsIntToken(1234)
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestLexNextLine()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("1234 \\ // comment \r\n 55"); // 나머지는 지원 안함

            var tokens = new List<QsToken>();

            var result = await lexer.LexIntAsync(context);
            tokens.Add(result.Token);

            result = await lexer.LexWhitespaceAsync(result.Context, false);
            tokens.Add(result.Token);

            result = await lexer.LexIntAsync(result.Context);
            tokens.Add(result.Token);

            var expectedTokens = new QsToken[] {
                new QsIntToken(1234),
                QsWhitespaceToken.Instance,                
                new QsIntToken(55)
            };

            Assert.Equal(expectedTokens, tokens);
        }
    }
}
