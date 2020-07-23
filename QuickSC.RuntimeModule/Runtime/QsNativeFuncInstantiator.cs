﻿using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, QsValue, ValueTask>;

    public class QsNativeFuncInstantiator
    {
        public QsMetaItemId FuncId { get; }

        bool bThisCall;
        Invoker invoker;

        public QsNativeFuncInstantiator(QsMetaItemId funcId, bool bThisCall, Invoker invoker)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            this.invoker = invoker;
        }

        public QsFuncInst Instantiate(QsDomainService domainService, QsFuncValue fv)
        {
            var typeEnv = domainService.MakeTypeEnv(fv);
            return new QsNativeFuncInst(bThisCall, (thisValue, argValues, result) => invoker.Invoke(domainService, typeEnv, thisValue, argValues, result));
        }
    }
}