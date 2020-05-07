using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    // int a
    public struct QsTypeAndName
    {
        public QsTypeExp Type { get; }
        public string Name { get; }

        // out int& a
        public QsTypeAndName(QsTypeExp type, string name)
        {
            Type = type;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeAndName param &&
                   EqualityComparer<QsTypeExp>.Default.Equals(Type, param.Type) &&
                   Name == param.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Name);
        }

        public static bool operator ==(QsTypeAndName left, QsTypeAndName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsTypeAndName left, QsTypeAndName right)
        {
            return !(left == right);
        }
    }
}