using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeExpTypeValueService
    {
        ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp;

        public QsTypeExpTypeValueService(ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp)
        {
            this.typeValuesByTypeExp = typeValuesByTypeExp;
        }

        public QsTypeValue GetTypeValue(QsTypeExp typeExp)
        {
            return typeValuesByTypeExp[typeExp];
        }
    }
}