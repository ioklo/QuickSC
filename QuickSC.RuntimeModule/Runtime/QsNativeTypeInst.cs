using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    class QsNativeTypeInst : QsTypeInst
    {
        QsTypeId typeId;
        QsValue defaultValue;
        ImmutableArray<QsTypeInst> typeArgs;

        public QsNativeTypeInst(QsTypeId typeId, QsValue defaultValue, ImmutableArray<QsTypeInst> typeArgs)
        {
            this.typeId = typeId;
            this.defaultValue = defaultValue;
            this.typeArgs = typeArgs;
        }
        
        public override QsValue MakeDefaultValue()
        {
            return defaultValue.MakeCopy();
        }
    }
}
