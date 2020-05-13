using System;
using System.Diagnostics;

namespace QuickSC
{
    public enum QsMemberFuncKind
    {
        Normal,
        Indexer,
    }

    public struct QsMemberFuncId
    {
        public QsMemberFuncKind Kind { get; }
        public string Name { get; }

        public QsMemberFuncId(QsMemberFuncKind kind)
        {
            Debug.Assert(kind != QsMemberFuncKind.Normal);

            Kind = kind;
            Name = string.Empty;
        }

        public QsMemberFuncId(string name)
        {
            Kind = QsMemberFuncKind.Normal;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsMemberFuncId id &&
                   Kind == id.Kind &&
                   Name == id.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Name);
        }

        public static bool operator ==(QsMemberFuncId left, QsMemberFuncId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsMemberFuncId left, QsMemberFuncId right)
        {
            return !(left == right);
        }
    }
}
