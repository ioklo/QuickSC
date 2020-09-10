using Gum.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsSyntaxNodeMetaItemService
    {
        private ImmutableDictionary<ISyntaxNode, QsMetaItemId> typeIdsByNode;
        private ImmutableDictionary<ISyntaxNode, QsMetaItemId> funcIdsByNode;

        public QsSyntaxNodeMetaItemService(
            ImmutableDictionary<ISyntaxNode, QsMetaItemId> typeIdsByNode, 
            ImmutableDictionary<ISyntaxNode, QsMetaItemId> funcIdsByNode)
        {
            this.typeIdsByNode = typeIdsByNode;
            this.funcIdsByNode = funcIdsByNode;
        }

        public QsMetaItemId GetTypeId(ISyntaxNode node)
        {
            return typeIdsByNode[node];
        }

        public QsMetaItemId GetFuncId(ISyntaxNode node)
        {
            return funcIdsByNode[node];
        }

    }
}