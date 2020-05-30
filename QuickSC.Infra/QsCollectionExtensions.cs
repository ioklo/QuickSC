using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public static class QsCollectionExtensions
    {
        public static ImmutableDictionary<TKey, TValue> ToImmutableWithComparer<TKey, TValue>(this Dictionary<TKey, TValue> dict)
        {
            return dict.ToImmutableDictionary(dict.Comparer);
        }
    }
}
