using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QuickSC.StaticAnalyzer
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsMetaItemId TypeId { get; }
        private Dictionary<QsMetaItemIdElem, QsMetaItemId> memberTypeIds;
        private ImmutableHashSet<string> enumElemNames;

        public QsTypeSkeleton(QsMetaItemId typeId, IEnumerable<string> enumElemNames)
        {
            TypeId = typeId;
            memberTypeIds = new Dictionary<QsMetaItemIdElem, QsMetaItemId>();
            this.enumElemNames = enumElemNames.ToImmutableHashSet();
        }

        public bool GetMemberTypeId(string name, int typeParamCount, [NotNullWhen(returnValue: true)] out QsMetaItemId? outTypeId)
        {
            return memberTypeIds.TryGetValue(new QsMetaItemIdElem(name, typeParamCount), out outTypeId);
        }

        public bool ContainsEnumElem(string name)
        {
            return enumElemNames.Contains(name);
        }

        public void AddMemberTypeId(string name, int typeParamCount, QsMetaItemId typeId)
        {
            Debug.Assert(!enumElemNames.Contains(name));
            memberTypeIds.Add(new QsMetaItemIdElem(name, typeParamCount), typeId);
        }
    }
}
