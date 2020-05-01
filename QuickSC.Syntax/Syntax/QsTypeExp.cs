using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsTypeExp
    {   
    }
 
    public class QsTypeIdExp : QsTypeExp
    {
        public string Name { get; }
        public QsTypeIdExp(string name) { Name = name; }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeIdExp exp &&
                   Name == exp.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static bool operator ==(QsTypeIdExp? left, QsTypeIdExp? right)
        {
            return EqualityComparer<QsTypeIdExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeIdExp? left, QsTypeIdExp? right)
        {
            return !(left == right);
        }
    }
}