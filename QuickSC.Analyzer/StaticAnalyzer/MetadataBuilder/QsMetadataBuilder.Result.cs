using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsMetadataBuilder
    {
        public class Result
        {
            public QsScriptMetadata ScriptMetadata { get; }
            public QsTypeExpTypeValueService TypeExpTypeValueService { get; }
            public ImmutableDictionary<QsFuncDecl, QsFuncInfo> FuncsByFuncDecl { get; }

            public Result(
                QsScriptMetadata scriptMetadata,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl)
            {
                ScriptMetadata = scriptMetadata;
                TypeExpTypeValueService = typeExpTypeValueService;
                FuncsByFuncDecl = funcsByFuncDecl;
            }
        }
    }
}