using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    class QsNativeTypeInst : QsTypeInst
    {
        QsTypeInst? baseTypeInst                                                  ;

        QsMetaItemId typeId;
        Func<QsValue> defaultValueFactory;
        QsTypeEnv typeEnv;

        public QsNativeTypeInst(QsTypeValue typeValue, QsTypeInst? baseTypeInst, QsMetaItemId typeId, Func<QsValue> defaultValueFactory, QsTypeEnv typeEnv)
            : base(typeValue)
        {
            this.baseTypeInst = baseTypeInst;
            this.typeId = typeId;
            this.defaultValueFactory = defaultValueFactory;
            this.typeEnv = typeEnv;
        }

        public override QsValue MakeDefaultValue()
        {
            return defaultValueFactory();
        }

        public override QsTypeInst? GetBaseTypeInst()
        {
            return baseTypeInst;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsNativeTypeInst inst &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(typeId, inst.typeId) &&
                   typeEnv.Equals(inst.typeEnv);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeId, typeEnv);
        }

        public static bool operator ==(QsNativeTypeInst? left, QsNativeTypeInst? right)
        {
            return EqualityComparer<QsNativeTypeInst?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsNativeTypeInst? left, QsNativeTypeInst? right)
        {
            return !(left == right);
        }
    }
}
