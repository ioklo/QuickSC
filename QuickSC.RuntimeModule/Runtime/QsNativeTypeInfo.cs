using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsNativeTypeInfo
    {
        public QsMetaItemId TypeId { get; }
        public ImmutableArray<string> TypeParams { get; }
        public QsTypeValue? BaseTypeValue { get; }
        public ImmutableArray<QsMetaItemId> MemberTypeIds { get; }
        public ImmutableArray<QsMetaItemId> StaticMemberFuncIds { get; }
        public ImmutableArray<QsMetaItemId> StaticMemberVarIds { get; }
        public ImmutableArray<QsMetaItemId> MemberFuncIds { get; }
        public ImmutableArray<QsMetaItemId> MemberVarIds { get; }

        public Func<QsValue> DefaultValueFactory { get; }

        public QsNativeTypeInfo(QsMetaItemId typeId,
            ImmutableArray<string> typeParams,
            QsTypeValue? baseTypeValue,
            ImmutableArray<QsMetaItemId> memberTypeIds,
            ImmutableArray<QsMetaItemId> staticMemberFuncIds,
            ImmutableArray<QsMetaItemId> staticMemberVarIds,
            ImmutableArray<QsMetaItemId> memberFuncIds,
            ImmutableArray<QsMetaItemId> memberVarIds,
            Func<QsValue> defaultValueFactory)
        {
            TypeId = typeId;
            TypeParams = typeParams;
            BaseTypeValue = baseTypeValue;
            MemberTypeIds = memberTypeIds;
            StaticMemberFuncIds = staticMemberFuncIds;
            StaticMemberVarIds = staticMemberVarIds;
            MemberFuncIds = memberFuncIds;
            MemberVarIds = memberVarIds;
            DefaultValueFactory = defaultValueFactory;
        }
    }
}
