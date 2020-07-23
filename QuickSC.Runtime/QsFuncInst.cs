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

        public delegate ValueTask InvokerDelegate(QsValue? thisValue, ImmutableArray<QsValue> args, QsValue retValue);
        InvokerDelegate Invoker;

        public QsNativeFuncInst(bool bThisCall, InvokerDelegate Invoker)
        {
            this.bThisCall = bThisCall;
            this.Invoker = Invoker;
        }

        public ValueTask CallAsync(QsValue? thisValue, ImmutableArray<QsValue> args, QsValue result)
        {
            Debug.Assert(bThisCall == (thisValue != null));
            return Invoker(thisValue, args, result);
        }
    }
}
