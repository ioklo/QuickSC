using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC
{
    // (System.Runtime, System.X<,>.Y<,,>.T)
    public struct QsMetaItemId
    {
        public ImmutableArray<QsMetaItemIdElem> Elems { get; }  // 
        public QsName Name => Elems[Elems.Length - 1].Name;

        public QsMetaItemId(ImmutableArray<QsMetaItemIdElem> elems)
        {
            Elems = elems;
        }

        public QsMetaItemId(params QsMetaItemIdElem[] elems)
        {
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsMetaItemId id &&
                   QsSeqEqComparer.Equals(Elems, id.Elems);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            QsSeqEqComparer.AddHash(ref hashCode, Elems);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsMetaItemId left, QsMetaItemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsMetaItemId left, QsMetaItemId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendJoin('.', Elems);

            return sb.ToString();
        }

        public QsMetaItemId Append(QsMetaItemIdElem elem)
        {
            return new QsMetaItemId(Elems.Add(elem));
        }

        public QsMetaItemId Append(string name, int typeParamCount)
        {
            return new QsMetaItemId(Elems.Add(new QsMetaItemIdElem(QsName.Text(name), 0)));
        }

        public QsMetaItemId Append(QsSpecialName specialName, int typeParamCount)
        {
            return new QsMetaItemId(Elems.Add(new QsMetaItemIdElem(QsName.Special(specialName), 0)));
        }
    }
}