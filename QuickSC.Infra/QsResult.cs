using System;

namespace QuickSC
{
    public struct QsResult<TContext>
    {
        public static QsResult<TContext> Invalid() { throw new NotImplementedException(); }
        public static QsResult<TContext> Result(TContext context)
        {
            return new QsResult<TContext>(context);
        }
        
        public TContext Context { get; }

        public QsResult(TContext context)
        {
            Context = context;
        }
    }

    public struct QsResult<TValue, TContext>
    {
        public static QsResult<TValue, TContext> Invalid() { throw new NotImplementedException(); }
        public static QsResult<TValue, TContext> Result(TValue value, TContext context)
        {
            return new QsResult<TValue, TContext>(value, context);
        }

        public TValue Value { get; }
        public TContext Context { get; }

        public QsResult(TValue value, TContext context)
        {
            Value = value;
            Context = context;
        }
    }
}