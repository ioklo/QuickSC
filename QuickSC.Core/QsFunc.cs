using System;
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
        public QsTypeValue RetType { get; }
        public ImmutableArray<QsTypeValue> ArgTypes { get; }

        public QsFunc(QsFuncId funcId, bool bThisCall, string name, ImmutableArray<string> typeParams, QsTypeValue retType, ImmutableArray<QsTypeValue> argTypes)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            Name = name;
            TypeParams = typeParams;
            RetType = retType;
            ArgTypes = argTypes;
        }

        public QsFunc(QsFuncId funcId, bool bThisCall, string name, ImmutableArray<string> typeParams, QsTypeValue retType, params QsTypeValue[] argTypes)
        {
            FuncId = funcId;
            this.bThisCall = bThisCall;
            Name = name;
            TypeParams = typeParams;
            RetType = retType;
            ArgTypes = ImmutableArray.Create(argTypes);
        }
    }
}
