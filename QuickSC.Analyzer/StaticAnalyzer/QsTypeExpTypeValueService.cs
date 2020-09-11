using Gum.CompileTime;
using Gum.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeExpTypeValueService
    {
        ImmutableDictionary<TypeExp, TypeValue> typeValuesByTypeExp;

        public QsTypeExpTypeValueService(ImmutableDictionary<TypeExp, TypeValue> typeValuesByTypeExp)
        {
            this.typeValuesByTypeExp = typeValuesByTypeExp;
        }

        public TypeValue GetTypeValue(TypeExp typeExp)
        {
            return typeValuesByTypeExp[typeExp];
        }
    }
}