using QuickSC.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeAndFuncBuilder
    {
        class Context
        {
            public string ModuleName { get; }

            public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> TypeIdsByLocation { get; }
            public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> FuncIdsByLocation { get; }
            public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

            public TypeBuilder? TypeBuilder { get; set; }
            public List<QsTypeInfo> TypeInfos { get; } // All Types
            public List<QsFuncInfo> FuncInfos { get; } // All Funcs
            public List<QsVarInfo> VarInfos { get; } // Type의 Variable
            public Dictionary<QsFuncDecl, QsFuncInfo> FuncsByFuncDecl { get; }

            public Context(
                string moduleName,
                QsTypeSkelCollectResult skelResult,
                QsTypeEvalResult evalResult)
            {
                ModuleName = moduleName;
                TypeIdsByLocation = skelResult.TypeIdsByLocation;
                FuncIdsByLocation = skelResult.FuncIdsByLocation;
                TypeValuesByTypeExp = evalResult.TypeValuesByTypeExp;

                TypeBuilder = null;
                TypeInfos = new List<QsTypeInfo>();
                FuncInfos = new List<QsFuncInfo>();
                VarInfos = new List<QsVarInfo>();
                FuncsByFuncDecl = new Dictionary<QsFuncDecl, QsFuncInfo>(QsRefEqComparer<QsFuncDecl>.Instance);
            }
        }
    }
}