using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public struct QsLexerContext
    {
        public static QsLexerContext Make(QsBufferPosition pos)
        {
            return new QsLexerContext(pos);
        }
        
        public QsBufferPosition Pos { get; }

        private QsLexerContext(QsBufferPosition pos) 
        {   
            Pos = pos; 
        }
        
        public QsLexerContext UpdatePos(QsBufferPosition pos)
        {
            return new QsLexerContext(pos);
        }
    }
}
