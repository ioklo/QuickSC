using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC
{
    public class QsEnumInfo : IQsEnumInfo
    {
        public QsMetaItemId? OuterTypeId { get; }
        public QsMetaItemId TypeId { get; }

        ImmutableArray<string> typeParams;
        ImmutableDictionary<string, QsEnumElemInfo> elemInfosByName;
        QsEnumElemInfo defaultElemInfo;

        public QsEnumInfo(
            QsMetaItemId? outerTypeId,
            QsMetaItemId typeId,
            IEnumerable<string> typeParams,
            IEnumerable<QsEnumElemInfo> elemInfos)
        {
            OuterTypeId = outerTypeId;
            TypeId = typeId;
            this.typeParams = typeParams.ToImmutableArray();

            defaultElemInfo = elemInfos.First();
            this.elemInfosByName = elemInfos.ToImmutableDictionary(elemInfo => elemInfo.Name);
        }

        public IReadOnlyList<string> GetTypeParams()
        {
            return typeParams;
        }

        public QsTypeValue? GetBaseTypeValue()
        {
            return null;
        }

        public bool GetMemberTypeId(string name, [NotNullWhen(true)] out QsMetaItemId? outTypeId)
        {
            outTypeId = null;
            return false;
        }

        public bool GetMemberFuncId(QsName memberFuncId, [NotNullWhen(true)] out QsMetaItemId? outFuncId)
        {
            outFuncId = null;
            return false;
        }

        public bool GetMemberVarId(QsName name, [NotNullWhen(true)] out QsMetaItemId? outVarId)
        {
            outVarId = null;
            return false;
        }

        public bool GetElemInfo(string idName, [NotNullWhen(returnValue: true)] out QsEnumElemInfo? outElemInfo)
        {
            if (elemInfosByName.TryGetValue(idName, out var elemInfo))
            {
                outElemInfo = elemInfo;
                return true;
            }
            else
            {
                outElemInfo = null;
                return false;
            }
        }

        public QsEnumElemInfo GetDefaultElemInfo()
        {
            return defaultElemInfo;
        }
    }
}
