using System;
using System.Diagnostics;

namespace QuickSC
{
    public enum QsSpecialName
    {
        Normal,
        Indexer,
        AnonymousLambda, // use Name member
    }

    public struct QsName
    {
        public QsSpecialName Kind { get;  }
        public string Name { get; }

        static public QsName AnonymousLambda(string name)
        {
            return new QsName(QsSpecialName.AnonymousLambda, name);
        }

        public QsName(QsSpecialName kind, string name)
        {
            Debug.Assert(kind == QsSpecialName.AnonymousLambda);
            Kind = kind;
            Name = name;
        }

        public QsName(QsSpecialName kind)
        {
            Debug.Assert(kind != QsSpecialName.Normal);

            Kind = kind;
            Name = string.Empty;
        }

        public QsName(string name)
        {
            Kind = QsSpecialName.Normal;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsName id &&
                   Kind == id.Kind &&
                   Name == id.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Name);
        }

        public static bool operator ==(QsName left, QsName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsName left, QsName right)
        {
            return !(left == right);
        }
    }
}
