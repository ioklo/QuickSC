using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC
{
    // (System.Runtime, System.X<,>.Y<,,>.T)
    // Absolute, Relative 둘다
    public class QsMetaItemId
    {
        public QsMetaItemId? Outer { get; }
        private QsMetaItemIdElem elem;

        public QsName Name { get => elem.Name; }
        public int TypeParamCount { get => elem.TypeParamCount; }

        public static QsMetaItemId Make(QsMetaItemIdElem elem0, params QsMetaItemIdElem[] elems)
        {
            var curId = new QsMetaItemId(null, elem0);

            foreach (var elem in elems)
                curId = new QsMetaItemId(curId, elem);

            return curId;
        }

        public static QsMetaItemId Make(string name, int typeParamCount = 0)
        {
            return new QsMetaItemId(null, new QsMetaItemIdElem(QsName.MakeText(name), typeParamCount));
        }

        public static QsMetaItemId Make(QsName name, int typeParamCount = 0)
        {
            return new QsMetaItemId(null, new QsMetaItemIdElem(name, typeParamCount));
        }
        
        private QsMetaItemId(QsMetaItemId? outer, QsMetaItemIdElem elem)
        {
            Outer = outer;
            this.elem = elem;
        }

        public static bool operator ==(QsMetaItemId? left, QsMetaItemId? right)
        {
            return EqualityComparer<QsMetaItemId?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsMetaItemId? left, QsMetaItemId? right)
        {
            return !(left == right);
        }

        public void ToString(StringBuilder sb)
        {
            if (Outer != null)
            {
                Outer.ToString(sb);
                sb.Append(".");
            }

            elem.ToString(sb);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb);
            return sb.ToString();
        }

        public QsMetaItemId Append(QsMetaItemIdElem elem)
        {
            return new QsMetaItemId(this, elem);
        }
        
        public QsMetaItemId Append(string name, int typeParamCount = 0)
        {
            return new QsMetaItemId(this, new QsMetaItemIdElem(name, typeParamCount));
        }

        public QsMetaItemId Append(QsName name, int typeParamCount = 0)
        {
            return new QsMetaItemId(this, new QsMetaItemIdElem(name, typeParamCount));
        }

        public override bool Equals(object? obj)
        {
            return obj is QsMetaItemId id &&
                   EqualityComparer<QsMetaItemId?>.Default.Equals(Outer, id.Outer) &&
                   EqualityComparer<QsMetaItemIdElem>.Default.Equals(elem, id.elem);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Outer, elem);
        }
    }
}