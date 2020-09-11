using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Gum.Runtime
{
    class QsNativeTypeInst : TypeInst
    {
        // key
        TypeValue typeValue;
        Func<Value> defaultValueFactory;

        public QsNativeTypeInst(TypeValue typeValue, Func<Value> defaultValueFactory)
        {
            this.typeValue = typeValue;
            this.defaultValueFactory = defaultValueFactory;
        }

        public override Value MakeDefaultValue()
        {
            return defaultValueFactory();
        }

        public override TypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsNativeTypeInst inst &&
                   EqualityComparer<TypeValue>.Default.Equals(typeValue, inst.typeValue);
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
