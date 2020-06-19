using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsNativeVar
    {
        public QsMetaItemId VarId { get; }
        public QsTypeValue TypeValue { get; }

        public QsNativeVar(QsMetaItemId varId, QsTypeValue typeValue)
        {
            VarId = varId;
            TypeValue = typeValue;
        }
    }
}
