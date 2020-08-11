using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFuncInfo
    {
        public QsMetaItemId? OuterId { get; }
        public QsMetaItemId FuncId { get; }
        public bool bSeqCall { get; }
        public bool bThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }

        public QsFuncInfo(QsMetaItemId? outerId, QsMetaItemId funcId, bool bSeqCall, bool bThisCall, IReadOnlyList<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            OuterId = outerId;
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;            
            TypeParams = typeParams.ToImmutableArray();
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
        }

        public QsFuncInfo(QsMetaItemId? outerId, QsMetaItemId funcId, bool bSeqCall, bool bThisCall, IReadOnlyList<string> typeParams, QsTypeValue retTypeValues, params QsTypeValue[] paramTypeValues)
        {
            OuterId = outerId;
            FuncId = funcId;
            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            TypeParams = typeParams.ToImmutableArray();
            RetTypeValue = retTypeValues;
            ParamTypeValues = ImmutableArray.Create(paramTypeValues);
        }
    }
}
