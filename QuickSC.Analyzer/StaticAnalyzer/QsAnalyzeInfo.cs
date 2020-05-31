using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzeInfo
    {
        public ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> InfosByNode { get; }
        public ImmutableDictionary<QsFuncId, QsScriptFuncTemplate> FuncTemplatesById { get; }

        public QsAnalyzeInfo(
            ImmutableDictionary<IQsSyntaxNode, QsSyntaxNodeInfo> infosByNode,
            ImmutableDictionary<QsFuncId, QsScriptFuncTemplate> funcTemplatesById)
        {
            this.InfosByNode = infosByNode;
            this.FuncTemplatesById = funcTemplatesById;
        }
    }
}