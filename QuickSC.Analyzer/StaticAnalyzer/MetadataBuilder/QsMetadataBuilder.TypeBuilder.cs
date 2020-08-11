using System;
using System.Collections.Generic;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsMetadataBuilder
    {
        class TypeBuilder
        {
            private QsTypeValue.Normal thisTypeValue;
            private Dictionary<QsName, QsMetaItemId> memberTypeIds;
            private Dictionary<QsName, QsMetaItemId> memberFuncIds;
            private Dictionary<QsName, QsMetaItemId> memberVarIds;

            public TypeBuilder(QsTypeValue.Normal thisTypeValue)
            {
                this.thisTypeValue = thisTypeValue;

                memberTypeIds = new Dictionary<QsName, QsMetaItemId>();
                memberFuncIds = new Dictionary<QsName, QsMetaItemId>();
                memberVarIds = new Dictionary<QsName, QsMetaItemId>();
            }

            public QsTypeValue.Normal GetThisTypeValue()
            {
                return thisTypeValue;
            }
        }
    }
}