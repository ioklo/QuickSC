using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    public abstract class QsFuncInst
    {
        public abstract bool bThisCall { get; }
    }

    public class QsNativeFuncInst : QsFuncInst
    {
        public override bool bThisCall { get; }

        Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker;

        public QsNativeFuncInst(bool bThisCall, Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker)
        {
            this.bThisCall = bThisCall;
            this.Invoker = Invoker;
        }

        public ValueTask<QsValue> CallAsync(QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(bThisCall == (thisValue != null));
            return Invoker(thisValue, args);
        }
    }
}
