using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC
{
    // ReferenceEqualityComparer는 .net 5부터 지원
    public class QsRefEqComparer<T> : IEqualityComparer<T> where T : class
    {
        public static QsRefEqComparer<T> Instance { get; } = new QsRefEqComparer<T>();

        private QsRefEqComparer() { }

        public bool Equals(T x, T y)
        {
            return Object.ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

}
