using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeExpEvaluator
    {
        class Context
        {
            private QsMetadataService metadataService;
            private QsSyntaxNodeMetaItemService syntaxNodeMetaItemService;
            private ImmutableDictionary<QsMetaItemId, QsTypeSkeleton> typeSkeletonsByTypeId;
            private IQsErrorCollector errorCollector;

            private Dictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp;
            private ImmutableDictionary<string, QsTypeValue.TypeVar> typeEnv;

            public Context(
                QsMetadataService metadataService,
                QsSyntaxNodeMetaItemService syntaxNodeMetaItemService,
                IEnumerable<QsTypeSkeleton> typeSkeletons,
                IQsErrorCollector errorCollector)
            {
                this.metadataService = metadataService;
                this.syntaxNodeMetaItemService = syntaxNodeMetaItemService;
                this.typeSkeletonsByTypeId = typeSkeletons.ToImmutableDictionary(skeleton => skeleton.TypeId);
                this.errorCollector = errorCollector;

                typeValuesByTypeExp = new Dictionary<QsTypeExp, QsTypeValue>(QsRefEqComparer<QsTypeExp>.Instance);
                typeEnv = ImmutableDictionary<string, QsTypeValue.TypeVar>.Empty;
            }            

            public IEnumerable<QsTypeInfo> GetTypeInfos(QsMetaItemId metaItemId)
            {
                return metadataService.GetTypeInfos(metaItemId);
            }

            public QsMetaItemId GetTypeId(IQsSyntaxNode node)
            {
                return syntaxNodeMetaItemService.GetTypeId(node);
            }

            public QsMetaItemId GetFuncId(IQsSyntaxNode node)
            {
                return syntaxNodeMetaItemService.GetFuncId(node);
            }

            public bool GetSkeleton(QsMetaItemId metaItemId, out QsTypeSkeleton outTypeSkeleton)
            {
                return typeSkeletonsByTypeId.TryGetValue(metaItemId, out outTypeSkeleton);
            }

            public void AddError(IQsSyntaxNode node, string msg)
            {
                errorCollector.Add(node, msg);
            }

            public void AddTypeValue(QsTypeExp exp, QsTypeValue typeValue)
            {
                typeValuesByTypeExp.Add(exp, typeValue);
            }

            public ImmutableDictionary<QsTypeExp, QsTypeValue> GetTypeValuesByTypeExp()
            {
                return typeValuesByTypeExp.ToImmutableWithComparer();
            }

            public bool GetTypeVar(string name, [NotNullWhen(returnValue: true)] out QsTypeValue.TypeVar? typeValue)
            {
                return typeEnv.TryGetValue(name, out typeValue);
            }

            public void ExecInScope(QsMetaItemId itemId, IEnumerable<string> typeParams, Action action)
            {
                var prevTypeEnv = typeEnv;

                foreach (var typeParam in typeParams)
                {
                    typeEnv = typeEnv.SetItem(typeParam, QsTypeValue.MakeTypeVar(itemId, typeParam));
                }

                try
                {
                    action();
                }
                finally
                {
                    typeEnv = prevTypeEnv;
                }
            }
        }

    }
}
