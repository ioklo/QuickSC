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
        async ValueTask<QsLexerContext> MakeCommandModeContextAsync(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return QsLexerContext.Make(await buffer.MakePosition().NextAsync());
        }

        async ValueTask<IEnumerable<QsToken>> ProcessAsync(QsLexer lexer, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while(true)
            {
                var lexResult = await lexer.LexCommandModeAsync(context);
                if (!lexResult.HasValue || lexResult.Token is QsEndOfCommandToken) break;

                context = lexResult.Context;
                result.Add(lexResult.Token);
            }

            return result;
        }

        [Fact]
        public async Task TestLexerProcessStringExpInCommandMode()
        {
            var lexer = new QsLexer();
            var context = await MakeCommandModeContextAsync("ps${ccc}ddd");

            var tokens = new List<QsToken>();
            var result = await lexer.LexCommandModeAsync(context);
            tokens.Add(result.Token); // ps

            result = await lexer.LexCommandModeAsync(result.Context);
            tokens.Add(result.Token); // ${

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // ccc

            result = await lexer.LexNormalModeAsync(result.Context);
            tokens.Add(result.Token); // }

            result = await lexer.LexCommandModeAsync(result.Context);
            tokens.Add(result.Token); // ddd

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ps"),
                new QsDollarLBraceToken(),
                new QsIdentifierToken("ccc"),
                new QsRBraceToken(),
                new QsTextToken("ddd")
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestCommandModeLexCommandsAsync()
        {
            var lexer = new QsLexer();
            var context = await MakeCommandModeContextAsync("ls -al");

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ls"),
                new QsWhitespaceToken(),
                new QsTextToken("-al"),
            };

            Assert.Equal(expectedTokens, result);
        }

        [Fact]
        public async Task TestCommandModeLexCommandsWithLineSeparatorAsync()
        {
            var lexer = new QsLexer();
            var context = await MakeCommandModeContextAsync("ls -al\r\nbb");

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ls"),
                new QsWhitespaceToken(),
                new QsTextToken("-al"),
            };

            Assert.Equal(expectedTokens, result);
        }
    }
}
