using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public struct QsEvalResult<TValue>
    {
        public static QsEvalResult<TValue> Invalid = new QsEvalResult<TValue>();

        public bool HasValue { get; }
        public TValue Value { get; }
        public QsEvalContext Context { get; }
        public QsEvalResult(TValue value, QsEvalContext context)
        {
            HasValue = true;
            Value = value;
            Context = context;
        }
    }
}
