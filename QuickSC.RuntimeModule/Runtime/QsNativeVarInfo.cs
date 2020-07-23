using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsNativeVarInfo
    {
        public bool bStatic { get; }
        public QsMetaItemId VarId { get; }
        public QsTypeValue TypeValue { get; }

        public QsNativeVarInfo(bool bStatic, QsMetaItemId varId, QsTypeValue typeValue)
        {
            this.bStatic = bStatic;
            VarId = varId;
            TypeValue = typeValue;
        }
    }
}
