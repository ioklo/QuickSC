using System;

namespace QuickSC
{
    public struct QsTypeId
    {
        public int Value { get; }

        internal QsTypeId(int value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeId id &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsTypeId left, QsTypeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsTypeId left, QsTypeId right)
        {
            return !(left == right);
        }
    }
}