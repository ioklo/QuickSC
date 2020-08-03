using QuickSC.Syntax;
using System;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzeInfo
    {
        public int PrivateGlobalVarCount { get; }
        private ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode { get; }
        public ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> FuncTemplatesById { get; }

        public QsAnalyzeInfo(
            int privateGlobalVarCount,
            ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode,
            ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplatesById)
        {
            PrivateGlobalVarCount = privateGlobalVarCount;
            this.infosByNode = infosByNode;
            FuncTemplatesById = funcTemplatesById;
        }

        public TSyntaxNodeInfo GetNodeInfo<TSyntaxNodeInfo>(IQsSyntaxNode node) where TSyntaxNodeInfo : QsSyntaxNodeInfo
        {
            return (TSyntaxNodeInfo)infosByNode[node];
        }
    }
}