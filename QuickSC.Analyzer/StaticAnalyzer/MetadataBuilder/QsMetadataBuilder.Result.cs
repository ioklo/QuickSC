using Gum.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsMetadataBuilder
    {
        public class Result
        {
            public QsScriptMetadata ScriptMetadata { get; }
            public QsTypeExpTypeValueService TypeExpTypeValueService { get; }
            public ImmutableDictionary<FuncDecl, QsFuncInfo> FuncInfosByDecl { get; }
            public ImmutableDictionary<EnumDecl, QsEnumInfo> EnumInfosByDecl{ get; }

            public Result(
                QsScriptMetadata scriptMetadata,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<FuncDecl, QsFuncInfo> funcInfosbyDecl,
                ImmutableDictionary<EnumDecl, QsEnumInfo> enumInfosByDecl)
            {
                ScriptMetadata = scriptMetadata;
                TypeExpTypeValueService = typeExpTypeValueService;
                FuncInfosByDecl = funcInfosbyDecl;
                EnumInfosByDecl = enumInfosByDecl;
            }
        }
    }
}