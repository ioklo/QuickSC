using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public struct QsFuncId
    {
        public IQsMetadata? Metadata { get; }
        public int Value { get; }
        public QsFuncId(IQsMetadata? metadata, int value) { Metadata = metadata; Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncId id &&
                   EqualityComparer<IQsMetadata?>.Default.Equals(Metadata, id.Metadata) &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Metadata, Value);
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
