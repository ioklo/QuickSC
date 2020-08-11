using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC
{
    public class QsScriptMetadata : IQsMetadata
    {
        public string ModuleName { get; }

        private ImmutableDictionary<QsMetaItemId, QsTypeInfo> typeInfos;
        private ImmutableDictionary<QsMetaItemId, QsFuncInfo> funcInfos;
        private ImmutableDictionary<QsMetaItemId, QsVarInfo> varInfos;

        public QsScriptMetadata(string moduleName, IEnumerable<QsTypeInfo> typeInfos, IEnumerable<QsFuncInfo> funcInfos, IEnumerable<QsVarInfo> varInfos)
        {
            ModuleName = moduleName;

            this.typeInfos = typeInfos.ToImmutableDictionary(typeInfo => typeInfo.TypeId);
            this.funcInfos = funcInfos.ToImmutableDictionary(funcInfo => funcInfo.FuncId);
            this.varInfos = varInfos.ToImmutableDictionary(varInfo => varInfo.VarId);
        }

        public bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(true)] out QsFuncInfo? funcInfo)
        {
            return funcInfos.TryGetValue(id, out funcInfo);
        }

        public bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(true)] out QsTypeInfo? typeInfo)
        {
            return typeInfos.TryGetValue(id, out typeInfo);
        }

        public bool GetVarInfo(QsMetaItemId id, [NotNullWhen(true)] out QsVarInfo? varInfo)
        {
            return varInfos.TryGetValue(id, out varInfo);
        }
    }
}