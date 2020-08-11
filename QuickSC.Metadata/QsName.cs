using System;
using System.Diagnostics;

namespace QuickSC
{
    public enum QsSpecialName
    {
        Normal,
        IndexerGet,
        IndexerSet,
        AnonymousLambda, // use Name member

        OpInc,
        OpDec,
    }

    public static class QsSpecialNames
    {
        public static QsName IndexerGet { get; } = new QsName(QsSpecialName.IndexerGet, null);
        public static QsName IndexerSet { get; } = new QsName(QsSpecialName.IndexerGet, null);
        public static QsName OpInc { get; } = new QsName(QsSpecialName.OpInc, null);
        public static QsName OpDec { get; } = new QsName(QsSpecialName.OpDec, null);
    }

    public struct QsName
    {
        public QsSpecialName Kind { get;  }
        public string? Name { get; }

        public static QsName MakeAnonymousLambda(string name)
        {
            return new QsName(QsSpecialName.AnonymousLambda, name);
        }

        public static QsName MakeText(string name)
        {
            return new QsName(QsSpecialName.Normal, name);
        }

        internal QsName(QsSpecialName kind, string? name)
        {
            Kind = kind;
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

        public override string ToString()
        {
            switch(Kind)
            {
                case QsSpecialName.Normal: return Name!;
                case QsSpecialName.AnonymousLambda: return $"$Labmda{Name!}";
                default: return $"${Kind}";
            }
        }
    }
}
