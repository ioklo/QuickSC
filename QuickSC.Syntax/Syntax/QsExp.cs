using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsExp
    {
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
        public ImmutableArray<QsStringExpElement> Elements { get; }
        
        public QsStringExp(ImmutableArray<QsStringExpElement> elements)
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

    public class QsIntLiteralExp : QsExp
    {
        public int Value { get; }
        public QsIntLiteralExp(int value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIntLiteralExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIntLiteralExp? left, QsIntLiteralExp? right)
        {
            return EqualityComparer<QsIntLiteralExp>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIntLiteralExp? left, QsIntLiteralExp? right)
        {
            return !(left == right);
        }
    }

    public class QsBoolLiteralExp : QsExp
    {
        public bool Value { get; }
        public QsBoolLiteralExp(bool value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsBoolLiteralExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsBoolLiteralExp? left, QsBoolLiteralExp? right)
        {
            return EqualityComparer<QsBoolLiteralExp>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBoolLiteralExp? left, QsBoolLiteralExp? right)
        {
            return !(left == right);
        }
    }
}
