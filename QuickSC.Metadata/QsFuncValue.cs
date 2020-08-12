using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsFuncValue
    {
        public QsMetaItemId FuncId { get; }
        public QsTypeArgumentList TypeArgList { get; }

        public QsFuncValue(QsMetaItemId funcId, QsTypeArgumentList typeArgList)
        {
            FuncId = funcId;
            TypeArgList = typeArgList;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncValue value &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(FuncId, value.FuncId) &&
                   EqualityComparer<QsTypeArgumentList>.Default.Equals(TypeArgList, value.TypeArgList);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FuncId, TypeArgList);
        }

        public static bool operator ==(QsFuncValue? left, QsFuncValue? right)
        {
            return EqualityComparer<QsFuncValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncValue? left, QsFuncValue? right)
        {
            return !(left == right);
        }
    }
}
