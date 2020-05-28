using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzeInfo
    {
        public ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> InfosByNode { get; }

        public QsAnalyzeInfo(
            ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode)
        {
            this.InfosByNode = infosByNode;
        }
    }
}