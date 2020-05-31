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

        public override bool Equals(object? obj)
        {
            return obj is QsNativeTypeInst inst &&
                   EqualityComparer<QsTypeId>.Default.Equals(typeId, inst.typeId) &&
                   typeArgs.Equals(inst.typeArgs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeId, typeArgs);
        }

        public override QsValue MakeDefaultValue()
        {
            return defaultValue.MakeCopy();
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
