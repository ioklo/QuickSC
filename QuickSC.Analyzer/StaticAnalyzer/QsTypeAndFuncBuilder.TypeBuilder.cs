using System.Collections.Generic;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeAndFuncBuilder
    {
        class TypeBuilder
        {
            public QsTypeValue ThisTypeValue { get; }
            public Dictionary<QsName, QsMetaItemId> MemberTypeIds { get; }
            public Dictionary<QsName, QsMetaItemId> MemberFuncIds { get; }
            public Dictionary<QsName, QsMetaItemId> MemberVarIds { get; }

            public TypeBuilder(QsTypeValue thisTypeValue)
            {
                ThisTypeValue = thisTypeValue;

                MemberTypeIds = new Dictionary<QsName, QsMetaItemId>();
                MemberFuncIds = new Dictionary<QsName, QsMetaItemId>();
                MemberVarIds = new Dictionary<QsName, QsMetaItemId>();
            }
        }
    }
}