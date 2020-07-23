using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFuncInfo
    {
        public QsMetaItemId FuncId { get; }
        public bool bSeqCall { get; }
        public bool bThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }

        public QsFuncInfo(QsMetaItemId funcId, bool bSeqCall, bool bThisCall, IReadOnlyList<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;            
            TypeParams = typeParams.ToImmutableArray();
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
        }

        public QsFuncInfo(QsMetaItemId funcId, bool bSeqCall, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retTypeValues, params QsTypeValue[] paramTypeValues)
        {
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            TypeParams = typeParams;
            RetTypeValue = retTypeValues;
            ParamTypeValues = ImmutableArray.Create(paramTypeValues);
        }
    }
}
