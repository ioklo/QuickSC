using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, QsValue, ValueTask>;

    public class QsNativeFuncInfo
    {
        public QsMetaItemId FuncId { get; }
        public bool bSeqCall { get; }
        public bool bThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }
        
        public Invoker Invoker { get; }

        public QsNativeFuncInfo(
            QsMetaItemId funcId, 
            bool bSeqCall, 
            bool bThisCall, 
            ImmutableArray<string> typeParams, 
            QsTypeValue retTypeValue, 
            ImmutableArray<QsTypeValue> paramTypeValues,
            Invoker invoker)
        {
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            TypeParams = typeParams;
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
            Invoker = invoker;
        }
    }
}
