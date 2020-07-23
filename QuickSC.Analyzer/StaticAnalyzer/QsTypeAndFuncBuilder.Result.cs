using QuickSC.Syntax;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeAndFuncBuilder
    {
        public class Result
        {
            public ImmutableArray<QsTypeInfo> Types { get; }
            public ImmutableArray<QsFuncInfo> Funcs { get; }
            public ImmutableArray<QsVarInfo> Vars { get; }
            public ImmutableDictionary<QsFuncDecl, QsFuncInfo> FuncsByFuncDecl { get; }

            public Result(
                ImmutableArray<QsTypeInfo> types,
                ImmutableArray<QsFuncInfo> funcs,
                ImmutableArray<QsVarInfo> vars,
                ImmutableDictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl)
            {
                Types = types;
                Funcs = funcs;
                Vars = vars;

                FuncsByFuncDecl = funcsByFuncDecl;
            }
        }
    }
}