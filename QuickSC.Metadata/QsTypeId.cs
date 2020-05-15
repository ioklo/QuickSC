using System;
using System.Collections.Generic;

namespace QuickSC
{
    public struct QsTypeId
    {
        public IQsMetadata? Metadata { get; } // 어느 메타데이터에서부터 온건가
        public int Value { get; }

        public QsTypeId(IQsMetadata? metadata, int value) { Metadata = metadata;  Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeId id &&
                   EqualityComparer<IQsMetadata?>.Default.Equals(Metadata, id.Metadata) &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Metadata, Value);
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