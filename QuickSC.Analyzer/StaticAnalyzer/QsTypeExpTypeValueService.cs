using Gum.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeExpTypeValueService
    {
        ImmutableDictionary<TypeExp, QsTypeValue> typeValuesByTypeExp;

        public QsTypeExpTypeValueService(ImmutableDictionary<TypeExp, QsTypeValue> typeValuesByTypeExp)
        {
            this.typeValuesByTypeExp = typeValuesByTypeExp;
        }

        public QsTypeValue GetTypeValue(TypeExp typeExp)
        {
            return typeValuesByTypeExp[typeExp];
        }
    }
}