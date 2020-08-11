using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC.StaticAnalyzer
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsMetaItemId TypeId { get; }
        private Dictionary<QsMetaItemIdElem, QsMetaItemId> memberTypeIds;

        public QsTypeSkeleton(QsMetaItemId typeId)
        {
            TypeId = typeId;
            this.memberTypeIds = new Dictionary<QsMetaItemIdElem, QsMetaItemId>();
        }

        public bool GetMemberTypeId(string name, int typeParamCount, [NotNullWhen(returnValue: true)] out QsMetaItemId? outTypeId)
        {
            return memberTypeIds.TryGetValue(new QsMetaItemIdElem(name, typeParamCount), out outTypeId);
        }

        public void AddMemberTypeId(string name, int typeParamCount, QsMetaItemId typeId)
        {
            memberTypeIds.Add(new QsMetaItemIdElem(name, typeParamCount), typeId);
        }
    }
}
