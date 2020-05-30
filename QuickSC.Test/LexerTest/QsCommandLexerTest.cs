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
        async ValueTask<QsLexerContext> MakeContextAsync(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return QsLexerContext.Make(await buffer.MakePosition().NextAsync());
        }

        async ValueTask<IEnumerable<QsToken>> ProcessAsync(QsLexer lexer, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while(!context.Pos.IsReachEnd())
            {
                var lexResult = await lexer.LexCommandModeAsync(context);
                if (!lexResult.HasValue) break;

                context = lexResult.Context;
                result.Add(lexResult.Token);
            }

            return result;
        }

        async ValueTask<QsLexerContext> RepeatLexNormalAsync(List<QsToken> tokens, QsLexer lexer, QsLexerContext context, bool bSkipNewLine, int repeatCount)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                var result = await lexer.LexNormalModeAsync(context, bSkipNewLine);
                tokens.Add(result.Token); // ps
                context = result.Context;
            }

            return context;
        }

        async ValueTask<QsLexerContext> RepeatLexCommandAsync(List<QsToken> tokens, QsLexer lexer, QsLexerContext context, int repeatCount)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                var result = await lexer.LexCommandModeAsync(context);
                tokens.Add(result.Token); // ps
                context = result.Context;
            }

            return context;
        }
            

        [Fact]
        public async Task TestLexerProcessStringExpInCommandMode()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("  p$$s${ ccc } \"ddd $e  \r\n }");

            var tokens = new List<QsToken>();

            context = await RepeatLexCommandAsync(tokens, lexer, context, 2);
            context = await RepeatLexNormalAsync(tokens, lexer, context, false, 2);
            context = await RepeatLexCommandAsync(tokens, lexer, context, 6);
            
            var expectedTokens = new QsToken[]
            {
                new QsTextToken("  p$s"),
                QsDollarLBraceToken.Instance,
                new QsIdentifierToken("ccc"),
                QsRBraceToken.Instance,
                new QsTextToken(" \"ddd "),
                new QsIdentifierToken("e"),
                new QsTextToken("  "),
                QsNewLineToken.Instance,
                new QsTextToken(" "),
                QsRBraceToken.Instance,
            };

            Assert.Equal(expectedTokens, tokens);
        }

        [Fact]
        public async Task TestCommandModeLexCommandsAsync()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("ls -al");

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ls -al")
            };

            Assert.Equal(expectedTokens, result);
        }

        [Fact]
        public async Task TestLexMultilinesAsync()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync(@"
hello world \n

    hello    

}");
            var tokens = new List<QsToken>();
            await RepeatLexCommandAsync(tokens, lexer, context, 6);

            var expected = new List<QsToken>
            {
                QsNewLineToken.Instance,
                new QsTextToken("hello world \\n"), QsNewLineToken.Instance, // skip multi newlines
                new QsTextToken("    hello    "), QsNewLineToken.Instance,                
                QsRBraceToken.Instance
            };

            Assert.Equal(expected, tokens);
        }

        [Fact]
        public async Task TestCommandModeLexCommandsWithLineSeparatorAsync()
        {
            var lexer = new QsLexer();
            var context = await MakeContextAsync("ls -al\r\nbb");

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ls -al"),
                QsNewLineToken.Instance,
                new QsTextToken("bb"),
            };

            Assert.Equal(expectedTokens, result);
        }
    }
}
