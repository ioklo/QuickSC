using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzeInfo
    {
        public int PrivateGlobalVarCount { get; }
        public ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> InfosByNode { get; }
        public ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> FuncTemplatesById { get; }

        public QsAnalyzeInfo(
            int privateGlobalVarCount,
            ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode,
            ImmutableDictionary<QsMetaItemId, QsScriptFuncTemplate> funcTemplatesById)
        {
            PrivateGlobalVarCount = privateGlobalVarCount;
            InfosByNode = infosByNode;
            FuncTemplatesById = funcTemplatesById;
        }
    }
}