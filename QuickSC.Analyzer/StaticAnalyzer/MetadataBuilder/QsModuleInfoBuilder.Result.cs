using Gum.CompileTime;
using Gum.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsModuleInfoBuilder
    {
        public class Result
        {
            public ScriptModuleInfo ModuleInfo { get; }
            public QsTypeExpTypeValueService TypeExpTypeValueService { get; }
            public ImmutableDictionary<FuncDecl, FuncInfo> FuncInfosByDecl { get; }
            public ImmutableDictionary<EnumDecl, EnumInfo> EnumInfosByDecl{ get; }

            public Result(
                ScriptModuleInfo moduleInfo,
                QsTypeExpTypeValueService typeExpTypeValueService,
                ImmutableDictionary<FuncDecl, FuncInfo> funcInfosbyDecl,
                ImmutableDictionary<EnumDecl, EnumInfo> enumInfosByDecl)
            {
                ModuleInfo = moduleInfo;
                TypeExpTypeValueService = typeExpTypeValueService;
                FuncInfosByDecl = funcInfosbyDecl;
                EnumInfosByDecl = enumInfosByDecl;
            }
        }
    }
}