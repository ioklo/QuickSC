using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsVarInfo
    {   
        public QsMetaItemId? OuterId { get; }
        public QsMetaItemId VarId { get; }
        public bool bStatic { get; }
        public QsTypeValue TypeValue { get; }

        public QsVarInfo(QsMetaItemId? outerId, QsMetaItemId varId, bool bStatic, QsTypeValue typeValue)
        {
            OuterId = outerId;
            VarId = varId;
            this.bStatic = bStatic;
            TypeValue = typeValue;
        }
    }
}
