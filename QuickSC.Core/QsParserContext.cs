
using QuickSC.Token;
using System;

namespace QuickSC
{
    class QsParserContext
    {
        QsLexer lexer;
        int lexerPos;
        
        public QsParserContext(QsLexer lexer)
        {
            this.lexer = lexer;
        }

        public QsToken? NextToken()
        {
            var tokenResult = lexer.NextToken(lexerPos);
            if (tokenResult.HasValue)
            {
                lexerPos = tokenResult.Value.NextPos;
                return tokenResult.Value.Token;
            }

            return null;
        }

        public bool IsReachedEnd()
        {
            throw new NotImplementedException();
        }

        internal object GetState()
        {
            throw new NotImplementedException();
        }

        internal void SetState(object savedState)
        {
            throw new NotImplementedException();
        }
    }
}