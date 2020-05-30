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
        public string? Name { get; }

        static public QsName AnonymousLambda(string name)
        {
            return new QsName(QsSpecialName.AnonymousLambda, name);
        }

        static public QsName Special(QsSpecialName specialName)
        {
            return new QsName(specialName, null);
        }

        static public QsName Text(string name)
        {
            return new QsName(QsSpecialName.Normal, name);
        }

        private QsName(QsSpecialName kind, string? name)
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
                case QsSpecialName.Indexer: return "$Indexer";
                case QsSpecialName.AnonymousLambda: return $"$Labmda{Name!}";
                default: return string.Empty;
            }
        }
    }
}
