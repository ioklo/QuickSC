using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    public abstract class QsFuncInst
    {
    }

    public class QsNativeFuncInst : QsFuncInst
    {
        public bool bThisCall { get; }

        Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker;

        public QsNativeFuncInst(bool bThisCall, Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker)
        {
            this.bThisCall = bThisCall;
            this.Invoker = Invoker;
        }

        public ValueTask<QsValue> CallAsync(QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            if (!bThisCall)
                thisValue = null;

            return Invoker(thisValue, args);
        }
    }
}
