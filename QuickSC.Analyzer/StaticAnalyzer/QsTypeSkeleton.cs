using System.Collections.Generic;

namespace QuickSC.StaticAnalyzer
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsTypeId TypeId { get; }
        public Dictionary<QsNameElem, QsTypeSkeleton> MemberSkeletons { get; }

        public QsTypeSkeleton(QsTypeId typeId)
        {
            TypeId = typeId;
            MemberSkeletons = new Dictionary<QsNameElem, QsTypeSkeleton>();
        }
    }
}
