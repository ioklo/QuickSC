using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeSkeletonCollectorContext
    {
        public Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton> GlobalTypeSkeletons { get; }
        public Dictionary<QsTypeIdLocation, QsTypeId> TypeIdsByLocation { get; }
        public Dictionary<QsFuncIdLocation, QsFuncId> FuncIdsByLocation { get; }
        public Dictionary<QsTypeId, QsTypeSkeleton> TypeSkeletonsByTypeId { get; }

        public QsTypeSkeleton? ScopeSkeleton { get; set; }

        public QsTypeSkeletonCollectorContext()
        {
            GlobalTypeSkeletons = new Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton>();
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

    public class QsTypeSkeletonCollector
    {
        // TypeId 발급기
        QsTypeIdFactory typeIdFactory;
        QsFuncIdFactory funcIdFactory;

        public QsTypeSkeletonCollector(QsTypeIdFactory typeIdFactory, QsFuncIdFactory funcIdFactory)
        {
            this.typeIdFactory = typeIdFactory;
            this.funcIdFactory = funcIdFactory;
        }
        
        QsTypeSkeleton MakeSkeleton(QsTypeIdLocation loc, string name, int typeParamCount, QsTypeSkeletonCollectorContext context)
        {
            var typeId = typeIdFactory.MakeTypeId();

            context.TypeIdsByLocation.Add(loc, typeId);

            var skeleton = new QsTypeSkeleton(typeId, name, typeParamCount);
            context.TypeSkeletonsByTypeId.Add(typeId, skeleton);

            if (context.ScopeSkeleton != null)
                context.ScopeSkeleton.MemberSkeletons.Add((skeleton.Name, skeleton.TypeParamCount), skeleton);
            else
                context.GlobalTypeSkeletons.Add((skeleton.Name, skeleton.TypeParamCount), skeleton);

            return skeleton;
        }

        bool CollectEnumDecl(QsEnumDecl enumDecl, QsTypeSkeletonCollectorContext context)
        {            
            var skeleton = MakeSkeleton(QsTypeIdLocation.Make(enumDecl), enumDecl.Name, enumDecl.TypeParams.Length, context);

            // 여기서는 직접 
            var prevScopeSkeleton = context.ScopeSkeleton;
            context.ScopeSkeleton = skeleton;

            foreach (var elem in enumDecl.Elems)
                MakeSkeleton(QsTypeIdLocation.Make(elem), elem.Name, 0, context); // memberType은 타입파라미터가 없어야 한다

            context.ScopeSkeleton = prevScopeSkeleton;
            
            return true;
        }

        bool CollectFuncDecl(QsFuncDecl funcDecl, QsTypeSkeletonCollectorContext context)
        {
            var funcId = funcIdFactory.MakeFuncId();
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
    }
}
