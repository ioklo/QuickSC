using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    using Invoker = Func<DomainService, TypeArgumentList, Value?, IReadOnlyList<Value>, Value, ValueTask>;

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

        public FuncInst Instantiate(DomainService domainService, FuncValue fv)
        {
            return new NativeFuncInst(bThisCall, (thisValue, argValues, result) => invoker.Invoke(domainService, fv.TypeArgList, thisValue, argValues, result));
        }
    }
}