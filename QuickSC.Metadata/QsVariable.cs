using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsVariable
    {
        public bool bStatic { get; }
        public QsMetaItemId VarId { get; }
        public QsTypeValue TypeValue { get; }

        public QsVariable(bool bStatic, QsMetaItemId varId, QsTypeValue typeValue)
        {
            this.bStatic = bStatic;
            VarId = varId;
            TypeValue = typeValue;
        }
    }
}
