
using QuickSC.Token;
using System;
using System.Threading.Tasks;

namespace QuickSC
{
    class QsParserContext
    {
        QsNormalLexer normalLexer;
        QsCommandLexer commandLexer;
        QsBufferPosition pos;
        
        public QsParserContext(QsBufferPosition pos, QsNormalLexer lexer, QsCommandLexer commandLexer)
        {
            this.normalLexer = lexer;
            this.commandLexer = commandLexer;
            this.pos = pos;
        }

        public async ValueTask<QsToken?> NextTokenAsync()
        {
            var tokenResult = await normalLexer.GetNextTokenAsync(pos);
            if (tokenResult.HasValue)
            {
                pos = tokenResult.Value.NextPos;
                return tokenResult.Value.Token;
            }

            return null;
        }

        public async ValueTask<QsCommandToken?> GetNextCommandTokenAsync()
        {
            var tokenResult = await commandLexer.GetNextCommandTokenAsync(pos);
            if (tokenResult.HasValue)
            {
                pos = tokenResult.Value.NextPos;
                return tokenResult.Value.Token;
            }

            return null;
        }

        public async ValueTask<QsCommandArgToken?> GetNextArgTokenAsync()
        {
            var tokenResult = await commandLexer.GetNextArgTokenAsync(pos);

            if (tokenResult.HasValue)
            {
                pos = tokenResult.Value.NextPos;
                return tokenResult.Value.Token;
            }

            return null;
        }

        public bool IsReachedEnd()
        {
            return pos.IsReachEnd();
        }        

        public QsBufferPosition GetState()
        {
            return pos;
        }

        public void SetState(QsBufferPosition savedState)
        {
            pos = savedState;
        }
    }
}