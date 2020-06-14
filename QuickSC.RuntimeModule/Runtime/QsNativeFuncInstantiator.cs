using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsNativeFuncInstantiator
    {
        bool bThisCall;
        Invoker invoker;

        public QsNativeFuncInstantiator(bool bThisCall, Invoker invoker)
        {
            this.bThisCall = bThisCall;
            this.invoker = invoker;
        }

        public QsFuncInst Instantiate(QsDomainService domainService, QsFuncValue fv)
        {
            var typeEnv = domainService.MakeTypeEnv(fv);
            return new QsNativeFuncInst(bThisCall, (thisValue, argValues) => invoker.Invoke(domainService, typeEnv, thisValue, argValues));
        }
    }
}