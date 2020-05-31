using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<ImmutableArray<QsTypeInst>, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsNativeFunc
    {
        public QsFunc Func { get; }
        public Invoker Invoker { get; }
        public QsNativeFunc(QsFunc func, Invoker invoker)
        {
            Func = func;
            Invoker = invoker;
        }
    }
}
