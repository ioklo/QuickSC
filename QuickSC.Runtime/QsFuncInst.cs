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
        Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker;

        public QsNativeFuncInst(Func<QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>> Invoker)
        {
            this.Invoker = Invoker;
        }

        public ValueTask<QsValue> CallAsync(QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            // TODO: bStatic일때 확인 필요

            return Invoker(thisValue, args);
        }
    }
}
