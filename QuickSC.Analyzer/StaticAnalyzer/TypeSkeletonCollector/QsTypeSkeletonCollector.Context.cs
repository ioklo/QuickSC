using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeSkeletonCollector
    {
        public class Context
        {
            private Dictionary<IQsSyntaxNode, QsMetaItemId> typeIdsByNode { get; }
            private Dictionary<IQsSyntaxNode, QsMetaItemId> funcIdsByNode { get; }
            private List<QsTypeSkeleton> typeSkeletons { get; }            

            private QsTypeSkeleton? scopeSkeleton { get; set; }

            public Context()
            {
                typeIdsByNode = new Dictionary<IQsSyntaxNode, QsMetaItemId>();
                funcIdsByNode = new Dictionary<IQsSyntaxNode, QsMetaItemId>();
                typeSkeletons = new List<QsTypeSkeleton>();
                scopeSkeleton = null;
            }
            
            internal void AddTypeSkeleton(IQsSyntaxNode node, string name, int typeParamCount, IEnumerable<string> enumElemNames)
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

            public void AddFuncId(IQsSyntaxNode node, QsMetaItemId funcId)
            {
                funcIdsByNode.Add(node, funcId);
            }

            public ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> GetTypeIdsByNode()
            {
                return typeIdsByNode.ToImmutableDictionary();
            }

            public ImmutableDictionary<IQsSyntaxNode, QsMetaItemId> GetFuncIdsByNode()
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
