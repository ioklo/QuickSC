using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{   

    // 가장 외곽
    public class QsScript
    {
        public ImmutableArray<QsScriptElement> Elements { get; }
        public QsScript(ImmutableArray<QsScriptElement> elements)
        {
            Elements = elements;
        }

        public QsScript(params QsScriptElement[] elements)
        {
            Elements = ImmutableArray.Create(elements);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsScript script && Enumerable.SequenceEqual(Elements, script.Elements);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            foreach (var elem in Elements)
                hashCode.Add(elem);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsScript? left, QsScript? right)
        {
            return EqualityComparer<QsScript?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsScript? left, QsScript? right)
        {
            return !(left == right);
        }
    }
}