using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsVarValue
    {
        public QsTypeValue? Outer { get; }
        public QsMetaItemId VarId { get; }

        public QsVarValue(QsTypeValue? outer, QsMetaItemId varId)
        {
            Outer = outer;
            VarId = varId;
        }
    }
}
