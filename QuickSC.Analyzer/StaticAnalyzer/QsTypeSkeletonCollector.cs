using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeSkeletonCollectorContext
    {
        public Dictionary<QsMetadataIdLocation, QsMetaItemId> TypeIdsByLocation { get; }
        public Dictionary<QsMetadataIdLocation, QsMetaItemId> FuncIdsByLocation { get; }
        public Dictionary<QsMetaItemId, QsTypeSkeleton> TypeSkeletonsByTypeId { get; }

        public QsTypeSkeleton? ScopeSkeleton { get; set; }

        public QsTypeSkeletonCollectorContext()
        {            
            TypeIdsByLocation = new Dictionary<QsMetadataIdLocation, QsMetaItemId>();
            FuncIdsByLocation = new Dictionary<QsMetadataIdLocation, QsMetaItemId>();
            TypeSkeletonsByTypeId = new Dictionary<QsMetaItemId, QsTypeSkeleton>();
            ScopeSkeleton = null;
        }

        // 1. TypeId 부여 Ref 대신 Id부여
        // 2. 순회하며 QsType 만들기, 만들때 중간에 나타나는 타입들은 어떻게 처리할 것인가

        // class X
        //     Y<int>.P<short> p;    // QsTypeValue (null, TypeId, [int]), TypeId(P), [short]
        // 
        // class Y<T>
        //     class P<U>;
        // 


        // 1. "X" -> TypeSkeleton(TypeId, 0, []), "Y" -> TypeSkeleton(TypeId, 2, ["P" => TypeSkeleton("P", TypeId)]), 
        // 2. 
        // 

        // X, Y, Y.P

        // 각종 Decl -> TypeId        
        // (QsMetaItemId parentTypeId, 
    }

    public class QsTypeSkelCollectResult
    {
        public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> TypeIdsByLocation { get; }
        public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> FuncIdsByLocation { get; }
        public ImmutableDictionary<QsMetaItemId, QsTypeSkeleton> TypeSkeletonsByTypeId { get; }

        public QsTypeSkelCollectResult(
            ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> typeIdsByLocation,
            ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> funcIdsByLocation,
            ImmutableDictionary<QsMetaItemId, QsTypeSkeleton> typeSkeletonsByTypeId)
        {
            TypeIdsByLocation = typeIdsByLocation;
            FuncIdsByLocation = funcIdsByLocation;
            TypeSkeletonsByTypeId = typeSkeletonsByTypeId;
        }
    }

    public class QsTypeSkeletonCollector
    {
        public QsTypeSkeletonCollector()
        {
        }
        
        QsTypeSkeleton MakeSkeleton(QsMetadataIdLocation loc, string name, int typeParamCount, QsTypeSkeletonCollectorContext context)
        {
            var nameElem = new QsMetaItemIdElem(name, typeParamCount);

            QsMetaItemId typeId;
            if (context.ScopeSkeleton != null)
                typeId = context.ScopeSkeleton.TypeId.Append(nameElem);
            else
                typeId = new QsMetaItemId(nameElem);

            context.TypeIdsByLocation.Add(loc, typeId);

            var skeleton = new QsTypeSkeleton(typeId);
            context.TypeSkeletonsByTypeId.Add(typeId, skeleton);

            if (context.ScopeSkeleton != null)
                context.ScopeSkeleton.MemberSkeletons.Add(nameElem, skeleton);

            return skeleton;
        }

        bool CollectEnumDecl(QsEnumDecl enumDecl, QsTypeSkeletonCollectorContext context)
        {            
            var skeleton = MakeSkeleton(QsMetadataIdLocation.Make(enumDecl), enumDecl.Name, enumDecl.TypeParams.Length, context);

            // 여기서는 직접 
            var prevScopeSkeleton = context.ScopeSkeleton;
            context.ScopeSkeleton = skeleton;

            foreach (var elem in enumDecl.Elems)
            {
                MakeSkeleton(QsMetadataIdLocation.Make(elem), elem.Name, 0, context); // memberType은 타입파라미터가 없어야 한다

                if (0 < elem.Params.Length)
                {
                    var funcId = skeleton.TypeId.Append(elem.Name, 0);
                    context.FuncIdsByLocation[QsMetadataIdLocation.Make(elem)] = funcId;
                }
            }

            context.ScopeSkeleton = prevScopeSkeleton;
            
            return true;
        }

        bool CollectFuncDecl(QsFuncDecl funcDecl, QsTypeSkeletonCollectorContext context)
        {
            // TODO: 현재는 최상위만
            Debug.Assert(context.ScopeSkeleton == null);

            var funcId = new QsMetaItemId(new QsMetaItemIdElem(funcDecl.Name, funcDecl.TypeParams.Length));
            context.FuncIdsByLocation[QsMetadataIdLocation.Make(funcDecl)] = funcId;
            return true;
        }

        public bool CollectScript(QsScript script, QsTypeSkeletonCollectorContext context)
        {
            foreach (var scriptElem in script.Elements)
            {
                switch(scriptElem)
                {
                    case QsEnumDeclScriptElement enumElem:
                        if (!CollectEnumDecl(enumElem.EnumDecl, context))
                            return false;
                        break;

                    case QsFuncDeclScriptElement funcElem:
                        if (!CollectFuncDecl(funcElem.FuncDecl, context))
                            return false;
                        break;
                }
            }

            return true;
        }

        public bool CollectScript(QsScript script, IQsErrorCollector errorCollector, [NotNullWhen(returnValue: true)] out QsTypeSkelCollectResult? outResult)
        {
            var context = new QsTypeSkeletonCollectorContext();
            if (!CollectScript(script, context))
            {
                errorCollector.Add(script, $"타입 정보 모으기에 실패했습니다");
                outResult = null;
                return false;
            }

            outResult = new QsTypeSkelCollectResult(
                context.TypeIdsByLocation.ToImmutableDictionary(),
                context.FuncIdsByLocation.ToImmutableDictionary(),
                context.TypeSkeletonsByTypeId.ToImmutableDictionary());
            return true;
        }
    }
}
