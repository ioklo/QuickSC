using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    enum QsLexingMode // 과연 여기 딸린 데이터가 필요없을까
    {
        Deploted,
        Normal,
        String,
        InnerExp,
        Command,
    }

    struct QsLexerContext
    {
        public static QsLexerContext Make(QsBufferPosition pos)
        {
            return new QsLexerContext(ImmutableStack.Create(QsLexingMode.Normal), pos);
        }

        private ImmutableStack<QsLexingMode> LexingModeStack;
        public QsLexingMode LexingMode { get => LexingModeStack.Peek(); }
        public QsBufferPosition Pos { get; }

        private QsLexerContext(ImmutableStack<QsLexingMode> lexingModeStack, QsBufferPosition pos) 
        { 
            LexingModeStack = lexingModeStack; 
            Pos = pos; 
        }
        
        public QsLexerContext UpdatePos(QsBufferPosition pos)
        {
            return new QsLexerContext(LexingModeStack, pos);
        }

        public QsLexerContext PushMode(QsLexingMode mode, QsBufferPosition pos)
        {
            return new QsLexerContext(LexingModeStack.Push(mode), pos);
        }
        
        public QsLexerContext PopMode(QsBufferPosition pos)
        {
            return new QsLexerContext(LexingModeStack.Pop(), pos);
        }

        public QsLexerContext Update(QsLexingMode mode, QsBufferPosition pos)
        {
            return new QsLexerContext(LexingModeStack.Pop().Push(mode), pos);
        }

        public QsLexerContext UpdateMode(QsLexingMode mode)
        {
            return new QsLexerContext(LexingModeStack.Pop().Push(mode), Pos);
        }
    }
}
