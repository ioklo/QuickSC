using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{   
    public abstract class QsCallable
    {
    }

    public class QsNativeCallable : QsCallable
    {
        public Func<QsValue, ImmutableArray<QsValue>, ValueTask<QsValue?>> Invoker { get; }
        public QsNativeCallable(Func<QsValue, ImmutableArray<QsValue>, ValueTask<QsValue?>> invoker)
        {
            Invoker = invoker;
        }
    }
}

