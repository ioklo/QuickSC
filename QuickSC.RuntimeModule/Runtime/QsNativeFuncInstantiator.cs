using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, TypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    public class QsNativeFuncInstantiator
    {
        public ModuleItemId FuncId { get; }

        bool bThisCall;
        Invoker invoker;

        public QsNativeFuncInstantiator(ModuleItemId funcId, bool bThisCall, Invoker invoker)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            this.invoker = invoker;
        }

        public QsFuncInst Instantiate(QsDomainService domainService, FuncValue fv)
        {
            return new QsNativeFuncInst(bThisCall, (thisValue, argValues, result) => invoker.Invoke(domainService, fv.TypeArgList, thisValue, argValues, result));
        }
    }
}