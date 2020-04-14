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
            return QsLexerContext.Make(await buffer.MakePosition().NextAsync()).UpdateMode(QsLexingMode.Command);
        }

        async ValueTask<IEnumerable<QsToken>> ProcessAsync(QsLexer lexer, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while(true)
            {
                var lexResult = await lexer.LexAsync(context);
                if (!lexResult.HasValue) break;

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

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ps"),
                new QsBeginInnerExpToken(),
                new QsIdentifierToken("ccc"),
                new QsEndInnerExpToken(),
                new QsTextToken("ddd"),
                new QsEndOfCommandToken(),
                new QsEndOfFileToken()
            };

            Assert.Equal(expectedTokens, result);
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
                new QsEndOfCommandToken(),
                new QsEndOfFileToken()
            };

            Assert.Equal(expectedTokens, result);
        }
    }
}
