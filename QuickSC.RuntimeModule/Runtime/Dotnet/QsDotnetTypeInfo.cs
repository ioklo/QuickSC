using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace QuickSC.Runtime.Dotnet
{
    public class QsDotnetTypeInfo : IQsTypeInfo
    {
        public QsMetaItemId? OuterTypeId { get; }
        public QsMetaItemId TypeId { get; }

        // it must be open type
        TypeInfo typeInfo;

        public QsDotnetTypeInfo(QsMetaItemId typeId, TypeInfo typeInfo)
        {
            if (typeInfo.DeclaringType != null)
                OuterTypeId = QsDotnetMisc.MakeTypeId(typeInfo.DeclaringType);

            TypeId = typeId;

            this.typeInfo = typeInfo;
        }        

        public QsTypeValue? GetBaseTypeValue()
        {
            throw new NotImplementedException();
        }

        public bool GetMemberFuncId(QsName memberFuncId, [NotNullWhen(true)] out QsMetaItemId? outFuncId)
        {
            if (memberFuncId.Kind != QsSpecialName.Normal)
                throw new NotImplementedException();

            try
            {
                var methodInfo = typeInfo.GetMethod(memberFuncId.Name!, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                // NOTICE: type의 경우에는 typeargument에 nested가 다 나오더니, func의 경우 함수의 것만 나온다
                outFuncId = TypeId.Append(memberFuncId.Name!, methodInfo.GetGenericArguments().Length);
                return true;
            }
            catch(AmbiguousMatchException)
            {
                outFuncId = null;
                return false;
            }
        }

        public bool GetMemberTypeId(string name, [NotNullWhen(true)] out QsMetaItemId? outTypeId)
        {
            var memberType = typeInfo.GetNestedType(name);
            if (memberType == null)
            {
                outTypeId = null;
                return false;
            }

            outTypeId = TypeId.Append(name, memberType.GenericTypeArguments.Length - typeInfo.GenericTypeArguments.Length);
            return true;
        }

        public bool GetMemberVarId(QsName varName, [NotNullWhen(true)] out QsMetaItemId? outVarId)
        {
            var candidates = new List<MemberInfo>();
            var memberInfos = typeInfo.GetMember(varName.Name!, MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if (1 < memberInfos.Length || !(memberInfos[0] is FieldInfo fieldInfo))
            {
                outVarId = null;
                return false;
            }

            outVarId = TypeId.Append(varName.Name!, 0);
            return true;
        }

        public IReadOnlyList<string> GetTypeParams()
        {
            return typeInfo.GenericTypeParameters.Select(typeInfo => typeInfo.Name).ToImmutableArray();                
        }
    }
}
