using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.TypeExpEvaluator
{
    class QsTypeEvalContext
    {
        ImmutableDictionary<string, QsTypeValue> types;

        public QsTypeEvalContext(ImmutableDictionary<string, QsTypeValue> types)
        {
            this.types = types;
        }

        public QsTypeValue? GetTypeValue(string name)
        {
            if (types.TryGetValue(name, out var type))
                return type;

            return null;
        }
    }
}
