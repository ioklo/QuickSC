using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC
{
    public class QsTypeArgumentList
    {
        public QsTypeArgumentList? Outer { get; }
        public ImmutableArray<QsTypeValue> Args { get; }        

        public static QsTypeArgumentList Empty { get; } = new QsTypeArgumentList(null, Enumerable.Empty<QsTypeValue>());

        public static QsTypeArgumentList Make(params QsTypeValue[] typeArgs)
        {
            return new QsTypeArgumentList(null, typeArgs);
        }        

        public static QsTypeArgumentList Make(QsTypeValue[] typeArgs0, params QsTypeValue[][] typeArgList)
        {
            var curList = new QsTypeArgumentList(null, typeArgs0);

            foreach (var elem in typeArgList)
                curList = new QsTypeArgumentList(curList, elem);

            return curList;
        }

        public static QsTypeArgumentList Make(QsTypeArgumentList? outer, IEnumerable<QsTypeValue> typeArgs)
        {
            return new QsTypeArgumentList(outer, typeArgs);
        }

        public static QsTypeArgumentList Make(QsTypeArgumentList? outer, params QsTypeValue[] typeArgs)
        {
            return new QsTypeArgumentList(outer, typeArgs);
        }

        private QsTypeArgumentList(QsTypeArgumentList? outer, IEnumerable<QsTypeValue> args)
        {
            Outer = outer;
            Args = args.ToImmutableArray();
        }        

        public override bool Equals(object? obj)
        {
            return obj is QsTypeArgumentList list &&
                   EqualityComparer<QsTypeArgumentList?>.Default.Equals(Outer, list.Outer) &&
                   QsSeqEqComparer.Equals(Args, list.Args);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Outer, Args);
        }

        public static bool operator ==(QsTypeArgumentList? left, QsTypeArgumentList? right)
        {
            return EqualityComparer<QsTypeArgumentList?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeArgumentList? left, QsTypeArgumentList? right)
        {
            return !(left == right);
        }

        public int GetTotalLength()
        {
            if (Outer != null)
                return Outer.GetTotalLength() + Args.Length;

            return Args.Length;
        }
    }    
}
