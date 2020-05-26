using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QuickSC
{
    public static class QsSeqEqComparer
    {
        public static QsSeqEqComparer<IEnumerable<TElem>, TElem> Get<TElem>(IEnumerable<TElem> e)
        {
            return QsSeqEqComparer<IEnumerable<TElem>, TElem>.Instance;
        }

        public static void AddHash<TElem>(ref HashCode hashCode, IEnumerable<TElem> e)
        {
            hashCode.Add(e, QsSeqEqComparer<IEnumerable<TElem>, TElem>.Instance);
        }

        public static bool Equals<TElem>(IEnumerable<TElem> e1, IEnumerable<TElem> e2)
        {
            return QsSeqEqComparer<IEnumerable<TElem>, TElem>.Instance.Equals(e1, e2);
        }
    }

    public class QsSeqEqComparer<T, TElem> : IEqualityComparer<T> where T : IEnumerable<TElem>
    {
        public static QsSeqEqComparer<T, TElem> Instance { get; } = new QsSeqEqComparer<T, TElem>();

        public bool Equals(T x, T y)
        {
            return Enumerable.SequenceEqual(x, y);
        }

        public int GetHashCode(T obj)
        {
            var hashCode = new HashCode();

            foreach(var e in obj)
                hashCode.Add(e);

            return hashCode.ToHashCode();
        }
    }
}
