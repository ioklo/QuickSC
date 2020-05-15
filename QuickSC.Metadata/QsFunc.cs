﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFunc
    {
        public QsFuncId FuncId { get; }
        public bool bThisCall { get; } // thiscall이라면 첫번째 ArgType은 this type이다
        public string Name { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue RetTypeValue { get; }
        public ImmutableArray<QsTypeValue> ParamTypeValues { get; }

        public QsFunc(QsFuncId funcId, bool bThisCall, string name, ImmutableArray<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            Name = name;
            TypeParams = typeParams;
            RetTypeValue = retTypeValue;
            ParamTypeValues = paramTypeValues;
        }

        public QsFunc(QsFuncId funcId, bool bThisCall, string name, ImmutableArray<string> typeParams, QsTypeValue retTypeValues, params QsTypeValue[] paramTypeValues)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            Name = name;
            TypeParams = typeParams;
            RetTypeValue = retTypeValues;
            ParamTypeValues = ImmutableArray.Create(paramTypeValues);
        }
    }
}
