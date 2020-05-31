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
        public string ModuleName { get; }
        public Dictionary<QsNameElem, QsTypeSkeleton> GlobalTypeSkeletons { get; }
        public Dictionary<QsTypeIdLocation, QsTypeId> TypeIdsByLocation { get; }
        public Dictionary<QsFuncIdLocation, QsFuncId> FuncIdsByLocation { get; }
        public Dictionary<QsTypeId, QsTypeSkeleton> TypeSkeletonsByTypeId { get; }

        public QsTypeSkeleton? ScopeSkeleton { get; set; }

        public QsTypeSkeletonCollectorContext(string moduleName)
        {
            ModuleName = moduleName;
            GlobalTypeSkeletons = new Dictionary<QsNameElem, QsTypeSkeleton>();
            TypeIdsByLocation = new Dictionary<QsTypeIdLocation, QsTypeId>();
            FuncIdsByLocation = new Dictionary<QsFuncIdLocation, QsFuncId>();
            TypeSkeletonsByTypeId = new Dictionary<QsTypeId, QsTypeSkeleton>();
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
        // (QsTypeId parentTypeId, 
    }

    public class QsTypeSkelCollectResult
    {
        public ImmutableDictionary<QsNameElem, QsTypeSkeleton> GlobalTypeSkeletons { get; }
        public ImmutableDictionary<QsTypeIdLocation, QsTypeId> TypeIdsByLocation { get; }
        public ImmutableDictionary<QsFuncIdLocation, QsFuncId> FuncIdsByLocation { get; }
        public ImmutableDictionary<QsTypeId, QsTypeSkeleton> TypeSkeletonsByTypeId { get; }

        public QsTypeSkelCollectResult(
            ImmutableDictionary<QsNameElem, QsTypeSkeleton> globalTypeSkeletons,
            ImmutableDictionary<QsTypeIdLocation, QsTypeId> typeIdsByLocation,
            ImmutableDictionary<QsFuncIdLocation, QsFuncId> funcIdsByLocation,
            ImmutableDictionary<QsTypeId, QsTypeSkeleton> typeSkeletonsByTypeId)
        {
            GlobalTypeSkeletons = globalTypeSkeletons;
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
        
        QsTypeSkeleton MakeSkeleton(QsTypeIdLocation loc, string name, int typeParamCount, QsTypeSkeletonCollectorContext context)
        {
            var nameElem = new QsNameElem(name, typeParamCount);

            QsTypeId typeId;
            if (context.ScopeSkeleton != null)
                typeId = new QsTypeId(context.ModuleName, context.ScopeSkeleton.TypeId.Elems.Add(nameElem));
            else
                typeId = new QsTypeId(context.ModuleName, nameElem);

            context.TypeIdsByLocation.Add(loc, typeId);

            var skeleton = new QsTypeSkeleton(typeId);
            context.TypeSkeletonsByTypeId.Add(typeId, skeleton);

            if (context.ScopeSkeleton != null)
                context.ScopeSkeleton.MemberSkeletons.Add(nameElem, skeleton);
            else
                context.GlobalTypeSkeletons.Add(nameElem, skeleton);

            return skeleton;
        }

        bool CollectEnumDecl(QsEnumDecl enumDecl, QsTypeSkeletonCollectorContext context)
        {            
            var skeleton = MakeSkeleton(QsTypeIdLocation.Make(enumDecl), enumDecl.Name, enumDecl.TypeParams.Length, context);

            // 여기서는 직접 
            var prevScopeSkeleton = context.ScopeSkeleton;
            context.ScopeSkeleton = skeleton;

            foreach (var elem in enumDecl.Elems)
            {
                MakeSkeleton(QsTypeIdLocation.Make(elem), elem.Name, 0, context); // memberType은 타입파라미터가 없어야 한다

                if (0 < elem.Params.Length)
                {
                    var funcId = new QsFuncId(context.ModuleName, skeleton.TypeId.Elems.Add(new QsNameElem(elem.Name, 0)));
                    context.FuncIdsByLocation[QsFuncIdLocation.Make(elem)] = funcId;
                }
            }

            context.ScopeSkeleton = prevScopeSkeleton;
            
            return true;
        }

        bool CollectFuncDecl(QsFuncDecl funcDecl, QsTypeSkeletonCollectorContext context)
        {
            // TODO: 현재는 최상위만
            Debug.Assert(context.ScopeSkeleton == null);

            var funcId = new QsFuncId(context.ModuleName, new QsNameElem(funcDecl.Name, funcDecl.TypeParams.Length));
            context.FuncIdsByLocation[QsFuncIdLocation.Make(funcDecl)] = funcId;
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

        public bool CollectScript(string moduleName, QsScript script, IQsErrorCollector errorCollector, [NotNullWhen(returnValue: true)] out QsTypeSkelCollectResult? outResult)
        {
            var context = new QsTypeSkeletonCollectorContext(moduleName);
            if (!CollectScript(script, context))
            {
                errorCollector.Add(script, $"타입 정보 모으기에 실패했습니다");
                outResult = null;
                return false;
            }

            outResult = new QsTypeSkelCollectResult(
                context.GlobalTypeSkeletons.ToImmutableDictionary(),
                context.TypeIdsByLocation.ToImmutableDictionary(),
                context.FuncIdsByLocation.ToImmutableDictionary(),
                context.TypeSkeletonsByTypeId.ToImmutableDictionary());
            return true;
        }
    }
}
