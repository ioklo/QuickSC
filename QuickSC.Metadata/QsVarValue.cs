using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace QuickSC
{
    public class QsVarValue
    {
        public QsMetaItemId VarId { get; }
        public QsTypeArgumentList OuterTypeArgList { get; } // variable은 자체로 타입 인자가 없으므로, TypeValue, FuncValue랑 다르게 outer부터 시작한다

        public QsVarValue(QsMetaItemId varId, QsTypeArgumentList outerTypeArgList)
        {
            VarId = varId;
            OuterTypeArgList = outerTypeArgList;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarValue value &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(VarId, value.VarId) &&
                   EqualityComparer<QsTypeArgumentList>.Default.Equals(OuterTypeArgList, value.OuterTypeArgList);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VarId, OuterTypeArgList);
        }

        public static bool operator ==(QsVarValue? left, QsVarValue? right)
        {
            return EqualityComparer<QsVarValue>.Default.Equals(left, right);
        }

        public static bool operator !=(QsVarValue? left, QsVarValue? right)
        {
            return !(left == right);
        }
    }
}
