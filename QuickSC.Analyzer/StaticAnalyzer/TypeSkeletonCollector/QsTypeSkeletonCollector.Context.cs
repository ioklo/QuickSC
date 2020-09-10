using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeSkeletonCollector
    {
        public class Context
        {
            private Dictionary<ISyntaxNode, QsMetaItemId> typeIdsByNode { get; }
            private Dictionary<ISyntaxNode, QsMetaItemId> funcIdsByNode { get; }
            private List<QsTypeSkeleton> typeSkeletons { get; }            

            private QsTypeSkeleton? scopeSkeleton { get; set; }

            public Context()
            {
                typeIdsByNode = new Dictionary<ISyntaxNode, QsMetaItemId>();
                funcIdsByNode = new Dictionary<ISyntaxNode, QsMetaItemId>();
                typeSkeletons = new List<QsTypeSkeleton>();
                scopeSkeleton = null;
            }
            
            internal void AddTypeSkeleton(ISyntaxNode node, string name, int typeParamCount, IEnumerable<string> enumElemNames)
            {
                QsMetaItemId typeId;                
                
                if (scopeSkeleton != null)
                    typeId = scopeSkeleton.TypeId.Append(name, typeParamCount);
                else
                    typeId = QsMetaItemId.Make(name, typeParamCount);

                typeIdsByNode.Add(node, typeId);
                typeSkeletons.Add(new QsTypeSkeleton(typeId, enumElemNames));

                if (scopeSkeleton != null)
                    scopeSkeleton.AddMemberTypeId(name, typeParamCount, typeId);
            }

            public void AddFuncId(ISyntaxNode node, QsMetaItemId funcId)
            {
                funcIdsByNode.Add(node, funcId);
            }

            public ImmutableDictionary<ISyntaxNode, QsMetaItemId> GetTypeIdsByNode()
            {
                return typeIdsByNode.ToImmutableDictionary();
            }

            public ImmutableDictionary<ISyntaxNode, QsMetaItemId> GetFuncIdsByNode()
            {
                return funcIdsByNode.ToImmutableDictionary();
            }

            public ImmutableArray<QsTypeSkeleton> GetTypeSkeletons()
            {
                return typeSkeletons.ToImmutableArray();
            }
        }
    }
}
