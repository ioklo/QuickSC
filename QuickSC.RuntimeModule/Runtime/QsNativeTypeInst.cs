using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    class QsNativeTypeInst : QsTypeInst
    {
        QsTypeInst? baseTypeInst;

        QsTypeId typeId;
        QsValue defaultValue;
        QsTypeEnv typeEnv;

        public QsNativeTypeInst(QsTypeInst? baseTypeInst, QsTypeId typeId, QsValue defaultValue, QsTypeEnv typeEnv)
        {
            this.baseTypeInst = baseTypeInst;
            this.typeId = typeId;
            this.defaultValue = defaultValue;
            this.typeEnv = typeEnv;
        }

        public override QsValue MakeDefaultValue()
        {
            return defaultValue.MakeCopy();
        }

        public override QsTypeInst? GetBaseTypeInst()
        {
            return baseTypeInst;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsNativeTypeInst inst &&
                   EqualityComparer<QsTypeId>.Default.Equals(typeId, inst.typeId) &&
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
