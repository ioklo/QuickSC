using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    // int a
    public class QsFuncDeclParam
    {
        public QsTypeExp Type { get; }
        public string Name { get; }

        // out int& a
        public QsFuncDeclParam(QsTypeExp type, string name)
        {
            Type = type;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncDeclParam param &&
                   EqualityComparer<QsTypeExp>.Default.Equals(Type, param.Type) &&
                   Name == param.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Name);
        }

        public static bool operator ==(QsFuncDeclParam? left, QsFuncDeclParam? right)
        {
            return EqualityComparer<QsFuncDeclParam?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncDeclParam? left, QsFuncDeclParam? right)
        {
            return !(left == right);
        }
    }
}