using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFuncValue
    {
        public QsTypeValue? Outer { get; }
        public QsMetaItemId FuncId { get; }
        public ImmutableArray<QsTypeValue> TypeArgs { get; }

        public QsFuncValue(QsTypeValue? outer, QsMetaItemId funcId, IEnumerable<QsTypeValue> typeArgs)
        {
            Outer = outer;
            FuncId = funcId;
            TypeArgs = typeArgs.ToImmutableArray();
        }

        public QsFuncValue(QsTypeValue? outer, QsMetaItemId funcId, params QsTypeValue[] typeArgs)
        {
            Outer = outer;
            FuncId = funcId;
            TypeArgs = ImmutableArray.Create(typeArgs);
        }
    }
}
