using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFunc
    {
        public QsFuncId FuncId { get; }
        public bool bThisCall { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }

        public QsFunc(QsFuncId funcId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;            
            TypeParams = typeParams;
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
        }

        public QsFunc(QsFuncId funcId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retTypeValues, params QsTypeValue[] paramTypeValues)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            TypeParams = typeParams;
            RetTypeValue = retTypeValues;
            ParamTypeValues = ImmutableArray.Create(paramTypeValues);
        }
    }
}
