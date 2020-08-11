using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsSyntaxNodeMetaItemService
    {
        private ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> typeIdsByNode;
        private ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> funcIdsByNode;

        public QsSyntaxNodeMetaItemService(
            ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> typeIdsByNode, 
            ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> funcIdsByNode)
        {
            this.typeIdsByNode = typeIdsByNode;
            this.funcIdsByNode = funcIdsByNode;
        }

        public QsMetaItemId GetTypeId(IQsSyntaxNode node)
        {
            return typeIdsByNode[node];
        }

        public QsMetaItemId GetFuncId(IQsSyntaxNode node)
        {
            return funcIdsByNode[node];
        }

    }
}