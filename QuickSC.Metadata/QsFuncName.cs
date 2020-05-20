using System;
using System.Diagnostics;

namespace QuickSC
{
    public enum QsFuncNameKind
    {
        Normal,
        Indexer,
        Invoker, // Lambda
    }

    public struct QsFuncName
    {
        public QsFuncNameKind Kind { get; }
        public string Name { get; }

        public QsFuncName(QsFuncNameKind kind)
        {
            Debug.Assert(kind != QsFuncNameKind.Normal);

            Kind = kind;
            Name = string.Empty;
        }

        public QsFuncName(string name)
        {
            Kind = QsFuncNameKind.Normal;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncName id &&
                   Kind == id.Kind &&
                   Name == id.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Name);
        }

        public static bool operator ==(QsFuncName left, QsFuncName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsFuncName left, QsFuncName right)
        {
            return !(left == right);
        }
    }
}
