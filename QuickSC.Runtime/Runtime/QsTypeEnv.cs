using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsTypeEnv
    {
        public ImmutableArray<QsTypeValue> TypeValues { get; }

        public QsTypeEnv(ImmutableArray<QsTypeValue> typeEnv)
        {
            TypeValues = typeEnv;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeEnv env &&
                   Enumerable.SequenceEqual(TypeValues, env.TypeValues);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TypeValues);
        }

        public static bool operator ==(QsTypeEnv? left, QsTypeEnv? right)
        {
            return EqualityComparer<QsTypeEnv?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeEnv? left, QsTypeEnv? right)
        {
            return !(left == right);
        }
    }
}
