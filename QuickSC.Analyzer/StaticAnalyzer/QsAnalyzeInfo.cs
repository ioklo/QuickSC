using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzeInfo
    {
        // 실행을 위한 기본 정보,         
        public ImmutableDictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        // 위의 것들은 제거        
        public ImmutableDictionary<QsExp, QsFuncValue> FuncValuesByExp { get; }
        public ImmutableDictionary<QsForeachStmt, QsForeachInfo> ForeachInfosByForEachStmt { get; }
        public ImmutableDictionary<QsVarDecl, QsEvalVarDecl> EvalVarDeclsByVarDecl { get; }
        public ImmutableDictionary<IQsSyntaxNode, QsEvalInfo> EvalInfosByNode { get; }

        public QsAnalyzeInfo(
            ImmutableDictionary<QsExp, QsTypeValue> typeValuesByExp,
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,            
            ImmutableDictionary<QsExp, QsFuncValue> funcValuesByExp,
            ImmutableDictionary<QsForeachStmt, QsForeachInfo> foreachInfosByForEachStmt,
            ImmutableDictionary<QsVarDecl, QsEvalVarDecl> evalVarDeclsByVarDecl,
            ImmutableDictionary<IQsSyntaxNode, QsEvalInfo> evalInfosByNode)
        {
            this.TypeValuesByExp = typeValuesByExp;
            this.TypeValuesByTypeExp = typeValuesByTypeExp;            
            this.FuncValuesByExp = funcValuesByExp;
            this.ForeachInfosByForEachStmt = foreachInfosByForEachStmt;
            this.EvalVarDeclsByVarDecl = evalVarDeclsByVarDecl;
            this.EvalInfosByNode = evalInfosByNode;
        }
    }
}