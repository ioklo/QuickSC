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
            public ImmutableDictionary<QsFuncDecl, QsFuncInfo> FuncInfosByDecl { get; }
            public ImmutableDictionary<QsEnumDecl, QsEnumInfo> EnumInfosByDecl{ get; }

            public Result(
                QsScriptMetadata scriptMetadata,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcInfosbyDecl,
                ImmutableDictionary<QsEnumDecl, QsEnumInfo> enumInfosByDecl)
            {
                ScriptMetadata = scriptMetadata;
                TypeExpTypeValueService = typeExpTypeValueService;
                FuncInfosByDecl = funcInfosbyDecl;
                EnumInfosByDecl = enumInfosByDecl;
            }
        }
    }
}