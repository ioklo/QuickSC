using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsMetadataBuilder
    {
        class Context
        {
            private QsSyntaxNodeMetaItemService syntaxNodeMetaItemService;
            private QsTypeExpTypeValueService typeExpTypeValueService;

            private TypeBuilder? typeBuilder;
            private List<QsTypeInfo> typeInfos; // All Types
            private List<QsFuncInfo> funcInfos; // All Funcs
            private List<QsVarInfo> varInfos; // Type의 Variable
            private Dictionary<QsFuncDecl, QsFuncInfo> funcsByFuncDecl;

            public Context(
                QsSyntaxNodeMetaItemService syntaxNodeMetaItemService,
                QsTypeExpTypeValueService typeExpTypeValueService)
            {
                this.syntaxNodeMetaItemService = syntaxNodeMetaItemService;
                this.typeExpTypeValueService = typeExpTypeValueService;

                typeBuilder = null;
                typeInfos = new List<QsTypeInfo>();
                funcInfos = new List<QsFuncInfo>();
                varInfos = new List<QsVarInfo>();
                funcsByFuncDecl = new Dictionary<QsFuncDecl, QsFuncInfo>(QsRefEqComparer<QsFuncDecl>.Instance);
            }

            public QsMetaItemId GetTypeId(IQsSyntaxNode node)
            {
                return syntaxNodeMetaItemService.GetTypeId(node);
            }

            public QsTypeValue GetTypeValue(QsTypeExp typeExp)
            {
                return typeExpTypeValueService.GetTypeValue(typeExp);
            }

            public QsMetaItemId GetFuncId(IQsSyntaxNode node)
            {
                return syntaxNodeMetaItemService.GetFuncId(node);
            }

            public QsTypeValue.Normal? GetThisTypeValue()
            {
                if (typeBuilder == null)
                    return null;

                return typeBuilder.GetThisTypeValue();
            }

            public void AddFuncInfo(QsFuncDecl? funcDecl, QsFuncInfo funcInfo)
            {
                funcInfos.Add(funcInfo);
                if (funcDecl != null)
                    funcsByFuncDecl[funcDecl] = funcInfo;
            }

            public void AddVarInfo(QsVarInfo varInfo)
            {
                varInfos.Add(varInfo);
            }

            public ImmutableArray<QsTypeInfo> GetTypeInfos()
            {
                return typeInfos.ToImmutableArray();
            }

            public ImmutableArray<QsFuncInfo> GetFuncInfos()
            {
                return funcInfos.ToImmutableArray();
            }

            public ImmutableArray<QsVarInfo> GetVarInfos()
            {
                return varInfos.ToImmutableArray();
            }

            public ImmutableDictionary<QsFuncDecl, QsFuncInfo> GetFuncsByFuncDecl()
            {
                return funcsByFuncDecl.ToImmutableWithComparer();
            }
        }
    }
}