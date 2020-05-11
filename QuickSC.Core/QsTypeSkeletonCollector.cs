using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace QuickSC
{
    // TypeSkeleton 정보, 이름별 TypeId와 부속타입 정보, 타입 파라미터 개수
    public class QsTypeSkeleton
    {
        public QsTypeId TypeId { get; }
        public int TypeParamCount { get; }
        public Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton> MemberSkeletons { get; }

        public QsTypeSkeleton(QsTypeId typeId, int typeParamCount)
        {
            TypeId = typeId;
            TypeParamCount = typeParamCount;
            MemberSkeletons = new Dictionary<(string Name, int TypeParamCount), QsTypeSkeleton>();
        }
    }

    public abstract class QsTypeSkelTypeDecl { }
    class QsTypeSkeletonEnumTypeDecl : QsTypeSkelTypeDecl
    {
        public QsEnumDecl EnumDecl { get; }
        public QsTypeSkeletonEnumTypeDecl(QsEnumDecl enumDecl) { EnumDecl = enumDecl; }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeSkeletonEnumTypeDecl decl &&
                   Object.ReferenceEquals(EnumDecl, decl.EnumDecl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EnumDecl);
        }

        public static bool operator ==(QsTypeSkeletonEnumTypeDecl? left, QsTypeSkeletonEnumTypeDecl? right)
        {
            return EqualityComparer<QsTypeSkeletonEnumTypeDecl>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeSkeletonEnumTypeDecl? left, QsTypeSkeletonEnumTypeDecl? right)
        {
            return !(left == right);
        }
    }

    public class QsTypeSkeletonCollectorContext
    {
        public Dictionary<string, QsTypeSkeleton> RootSkeletons { get; }
        public Dictionary<QsTypeSkelTypeDecl, QsTypeId> CollectedTypeIds { get; }
        public QsTypeSkeleton? ScopeSkeleton { get; }

        public QsTypeSkeletonCollectorContext()
        {
            RootSkeletons = new Dictionary<string, QsTypeSkeleton>();
            CollectedTypeIds = new Dictionary<QsTypeSkelTypeDecl, QsTypeId>();
            ScopeSkeleton = null;
        }

        public void AddSkeleton(string name, QsTypeSkeleton skeleton)
        {
            if (ScopeSkeleton != null)
                ScopeSkeleton.ChildTypeSkeletons.Add(name, skeleton);
            else
                RootSkeletons.Add(name, skeleton);
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

        public QsTypeSkeletonCollector(QsTypeIdFactory typeIdFactory)
        {
            this.typeIdFactory = typeIdFactory;
        }

        bool CollectEnumDecl(QsEnumDecl enumDecl, QsTypeSkeletonCollectorContext context)
        {
            var newTypeId = typeIdFactory.MakeTypeId();
            var skeleton = new QsTypeSkeleton(newTypeId, enumDecl.TypeParams.Length);
            context.AddSkeleton(enumDecl.Name, skeleton);

            // 여기서는 직접 
            foreach (var elem in enumDecl.Elems)
            {
                var newChildTypeId = typeIdFactory.MakeTypeId();
                var childSkeleton = new QsTypeSkeleton(newChildTypeId, 0); // childType은 타입파라미터가 없어야 한다

                skeleton.MemberSkeletons.Add(elem.Name, childSkeleton);
            }
            
            return true;
        }

        public bool CollectScript(QsScript script, QsTypeSkeletonCollectorContext context)
        {
            foreach(var scriptElem in script.Elements)
            {
                switch(scriptElem)
                {
                    case QsEnumDeclScriptElement enumElem:
                        if (!CollectEnumDecl(enumElem.EnumDecl, context))
                            return false;
                        break;
                }
            }

            return true;
        }
    }
}
