using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsVariable
    {
        public QsVarId VarId { get; }
        public QsTypeValue TypeValue { get; }

        public QsVariable(QsVarId varId, QsTypeValue typeValue)
        {
            VarId = varId;
            TypeValue = typeValue;
        }
    }
}
