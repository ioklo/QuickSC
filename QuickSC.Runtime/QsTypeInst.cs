using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeInst
    {
        public QsTypeValue TypeValue { get; }

        public QsTypeInst(QsTypeValue typeValue)
        {
            TypeValue = typeValue;
        }

        public abstract QsTypeInst? GetBaseTypeInst();
        public abstract QsValue MakeDefaultValue();
    }
}
