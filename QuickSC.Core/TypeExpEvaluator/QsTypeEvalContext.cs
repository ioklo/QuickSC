using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.TypeExpEvaluator
{
    public class QsTypeEvalContext
    {
        public ImmutableDictionary<QsTypeId, QsTypeSkeleton> TypeSkeletons { get; }
        public ImmutableDictionary<(string Name, int TypeParamCount), QsTypeSkeleton> GlobalTypeSkeletons { get; }
        public ImmutableDictionary<object, QsTypeId> TypeIdsByTypeDecl { get; } // TODO: 현재 안쓰인다

        public Dictionary<QsTypeExp, QsTypeValue> TypeExpTypeValues { get; }
        public ImmutableDictionary<string, QsTypeValue> TypeEnv { get; set; }
        public List<(object obj, string message)> Errors { get; }

        public QsTypeEvalContext(
            ImmutableDictionary<QsTypeId, QsTypeSkeleton> typeSkeletons,
            ImmutableDictionary<(string Name, int TypeParamCount), QsTypeSkeleton> globalTypeSkeletons,
            ImmutableDictionary<object, QsTypeId> typeIdsByTypeDecl)
        {
            TypeSkeletons = typeSkeletons;
            GlobalTypeSkeletons = globalTypeSkeletons;
            TypeIdsByTypeDecl = typeIdsByTypeDecl;
            TypeExpTypeValues = new Dictionary<QsTypeExp, QsTypeValue>();
            TypeEnv = ImmutableDictionary<string, QsTypeValue>.Empty;
            Errors = new List<(object obj, string message)>();
        }

        public void UpdateTypeVar(string name, QsTypeValue typeValue)
        {
            TypeEnv = TypeEnv.SetItem(name, typeValue);
        }
    }
}
