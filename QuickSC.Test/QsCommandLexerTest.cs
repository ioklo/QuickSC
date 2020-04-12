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
        QsLexerContext ToContext(string text)
        {
            var buffer = new QsBuffer(new StringReader(text));
            return QsLexerContext.Make(buffer.MakePosition()).UpdateMode(QsLexingMode.Command);
        }

        async ValueTask<IEnumerable<QsToken>> ProcessAsync(QsLexer lexer, QsLexerContext context)
        {
            var result = new List<QsToken>();

            while(true)
            {
                var lexResult = await lexer.LexAsync(context);
                if (!lexResult.HasValue) break;

                result.Add(lexResult.Token);
            }

            return result;
        }

        [Fact]
        public async ValueTask TestLexerProcessTextInCommandMode()
        {
            var lexer = new QsLexer();
            var result = await lexer.LexAsync(ToContext("abcd"));
            
            Assert.True(result.HasValue);
            Assert.Equal(new QsTextToken("abcd"), result.Token);
        }
        
        [Fact]
        public async ValueTask TestLexerProcessStringExpInCommandMode()
        {
            var lexer = new QsLexer();
            var context = ToContext("ps${ccc}ddd");

            var result = await ProcessAsync(lexer, context);

            var expectedTokens = new QsToken[]
            {
                new QsTextToken("ps"),
                new QsBeginStringToken(),
                new QsTextToken("ccc"),
                new QsEndStringToken(),
                new QsTextToken("ddd"),
                new QsEndOfCommandTokenToken(),
                new QsEndOfFileToken()
            };

            Assert.Equal(expectedTokens, result);
        }
    }
}
