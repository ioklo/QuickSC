using System.Collections.Generic;

namespace QuickSC.StaticAnalyzer
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsMetaItemId TypeId { get; }
        public Dictionary<QsMetaItemIdElem, QsTypeSkeleton> MemberSkeletons { get; }

        public QsTypeSkeleton(QsMetaItemId typeId)
        {
            TypeId = typeId;
            MemberSkeletons = new Dictionary<QsMetaItemIdElem, QsTypeSkeleton>();
        }
    }
}
