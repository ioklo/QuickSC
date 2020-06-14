using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    public class QsNativeFunc
    {
        public QsMetaItemId FuncId { get; }
        public bool bSeqCall { get; }
        public bool bThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }
        
        public QsNativeFuncInstantiator Instantiator { get; }

        public QsNativeFunc(
            QsMetaItemId funcId, 
            bool bSeqCall, 
            bool bThisCall, 
            ImmutableArray<string> typeParams, 
            QsTypeValue retTypeValue, 
            ImmutableArray<QsTypeValue> paramTypeValues,
            QsNativeFuncInstantiator instantiator)
        {
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            TypeParams = typeParams;
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
            Instantiator = instantiator;
        }
    }
}
