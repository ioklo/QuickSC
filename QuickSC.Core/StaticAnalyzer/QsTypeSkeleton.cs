using System.Collections.Generic;

namespace QuickSC.StaticAnalyzer
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsTypeId TypeId { get; }
        public string Name { get; }
        public int TypeParamCount { get; }
        public Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton> MemberSkeletons { get; }

        public QsTypeSkeleton(QsTypeId typeId, string name, int typeParamCount)
        {
            TypeId = typeId;
            Name = name;
            TypeParamCount = typeParamCount;
            MemberSkeletons = new Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton>();
        }
    }
}
