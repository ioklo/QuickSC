using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsExp
    {
    }

    public class QsCommandExp : QsExp
    {
        List<QsExp> Exps { get; }
        public QsCommandExp(List<QsExp> exps) { Exps = exps; }
    }

    public class QsIdentifierExp : QsExp
    {
        public string Value;
        public QsIdentifierExp(string value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIdentifierExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIdentifierExp? left, QsIdentifierExp? right)
        {
            return EqualityComparer<QsIdentifierExp>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIdentifierExp? left, QsIdentifierExp? right)
        {
            return !(left == right);
        }
    }

    public class QsStringExp : QsExp
    {
        public List<QsStringExpElement> Elements { get; }
        
        public QsStringExp(List<QsStringExpElement> elements)
        {
            Elements = elements;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsStringExp exp && Enumerable.SequenceEqual(Elements, exp.Elements);                   
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            foreach (var elem in Elements)
                hashCode.Add(elem);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsStringExp? left, QsStringExp? right)
        {
            return EqualityComparer<QsStringExp>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStringExp? left, QsStringExp? right)
        {
            return !(left == right);
        }
    }
}
