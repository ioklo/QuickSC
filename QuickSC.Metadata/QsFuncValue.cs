using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFuncValue
    {
        public QsTypeValue? Outer { get; }
        public QsFuncId FuncId { get; }
        public ImmutableArray<QsTypeValue> TypeArgs { get; }

        public QsFuncValue(QsTypeValue? outer, QsFuncId funcId, ImmutableArray<QsTypeValue> typeArgs)
        {
            Outer = outer;
            FuncId = funcId;
            TypeArgs = typeArgs;
        }
    }
}
