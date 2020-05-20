using System.Collections.Immutable;

namespace QuickSC.Runtime
{
    public class QsTypeInstEnv
    {
        public ImmutableDictionary<QsTypeVarTypeValue, QsTypeInst> TypeEnv { get; }

    }
}