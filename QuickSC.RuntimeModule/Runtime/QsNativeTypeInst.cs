using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    class QsNativeTypeInst : QsTypeInst
    {
        // key
        QsTypeValue typeValue;
        Func<QsValue> defaultValueFactory;

        public QsNativeTypeInst(QsTypeValue typeValue, Func<QsValue> defaultValueFactory)
        {
            this.typeValue = typeValue;
            this.defaultValueFactory = defaultValueFactory;
        }

        public override QsValue MakeDefaultValue()
        {
            return defaultValueFactory();
        }

        public override QsTypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsNativeTypeInst inst &&
                   EqualityComparer<QsTypeValue>.Default.Equals(typeValue, inst.typeValue);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeValue);
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
