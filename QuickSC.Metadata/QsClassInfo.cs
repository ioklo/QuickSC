using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsClassInfo : QsDefaultTypeInfo, IQsClassInfo
    {
        public QsClassInfo(
            QsMetaItemId? outerTypeId,
            QsMetaItemId typeId,
            IEnumerable<string> typeParams,
            QsTypeValue? baseTypeValue,
            IEnumerable<QsMetaItemId> memberTypeIds,
            IEnumerable<QsMetaItemId> memberFuncIds,
            IEnumerable<QsMetaItemId> memberVarIds)
            : base(outerTypeId, typeId, typeParams, baseTypeValue, memberTypeIds, memberFuncIds, memberVarIds)
        {
        }
    }
}
