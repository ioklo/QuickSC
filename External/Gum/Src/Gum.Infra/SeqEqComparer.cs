﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Gum.Infra
{
    public static class SeqEqComparer
    {
        public static SeqEqComparer<IEnumerable<TElem>, TElem> Get<TElem>(IEnumerable<TElem> e)
        {
            return SeqEqComparer<IEnumerable<TElem>, TElem>.Instance;
        }

        public static void AddHash<TElem>(ref HashCode hashCode, IEnumerable<TElem> e)
        {
            hashCode.Add(e, SeqEqComparer<IEnumerable<TElem>, TElem>.Instance);
        }

        public static bool Equals<TElem>(IEnumerable<TElem> e1, IEnumerable<TElem> e2)
        {
            return SeqEqComparer<IEnumerable<TElem>, TElem>.Instance.Equals(e1, e2);
        }
    }

    public class SeqEqComparer<T, TElem> : IEqualityComparer<T> where T : IEnumerable<TElem>
    {
        public static SeqEqComparer<T, TElem> Instance { get; } = new SeqEqComparer<T, TElem>();

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
