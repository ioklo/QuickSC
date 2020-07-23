using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QuickSC
{
    public class MultiDictionary<TKey, TValue> where TValue : notnull
    {
        Dictionary<TKey, List<TValue>> dict;

        public MultiDictionary()
        {
            dict = new Dictionary<TKey, List<TValue>>();
        }

        public void Add(TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<TValue>();                
                dict.Add(key, list);
            }

            list.Add(value);
        }

        public bool GetSingleValue(TKey key, [MaybeNullWhen(returnValue: false)] out TValue outValue)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                outValue = default;
                return false;
            }

            if (list.Count != 1)
            {
                outValue = default;
                return false;
            }

            outValue = list[0];
            return true;
        }

        public IEnumerable<TValue> GetMultiValues(TKey key)
        {
            if (!dict.TryGetValue(key, out var list))
                yield break;

            foreach (var elem in list)
                yield return elem;
        }
    }
}
