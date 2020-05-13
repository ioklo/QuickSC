using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public struct QsFuncId
    {
        public int Value { get; }
        public QsFuncId(int  value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncId id &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsFuncId left, QsFuncId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsFuncId left, QsFuncId right)
        {
            return !(left == right);
        }
    }
}
