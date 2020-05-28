using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsVarValue
    {
        public QsTypeValue? Outer { get; }
        public QsVarId VarId { get; }

        public QsVarValue(QsTypeValue? outer, QsVarId varId)
        {
            Outer = outer;
            VarId = varId;
        }
    }
}
