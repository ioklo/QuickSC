using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsDotnetType : QsType
    {
        // it must be open type
        Type type;

        public QsDotnetType(QsMetaItemId typeId, Type type)
            : base(typeId)
        {
            this.type = type;
        }

        public override QsTypeValue? GetBaseTypeValue()
        {
            throw new NotImplementedException();
        }

        public override bool GetMemberFuncId(QsName memberFuncId, [NotNullWhen(true)] out (bool bStatic, QsMetaItemId FuncId)? outValue)
        {
            if (memberFuncId.Kind != QsSpecialName.Normal)
                throw new NotImplementedException();

            try
            {
                var methodInfo = type.GetMethod(memberFuncId.Name!, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                // NOTICE: type의 경우에는 typeargument에 nested가 다 나오더니, func의 경우 함수의 것만 나온다
                outValue = (methodInfo.IsStatic, TypeId.Append(memberFuncId.Name!, methodInfo.GetGenericArguments().Length));
                return true;
            }
            catch(AmbiguousMatchException)
            {
                outValue = null;
                return false;
            }
        }

        public override bool GetMemberTypeId(string name, [NotNullWhen(true)] out QsMetaItemId? outTypeId)
        {
            var memberType = type.GetNestedType(name);
            if (memberType == null)
            {
                outTypeId = null;
                return false;
            }

            outTypeId = TypeId.Append(name, memberType.GenericTypeArguments.Length - type.GenericTypeArguments.Length);
            return true;
        }

        public override bool GetMemberVarId(string varName, [NotNullWhen(true)] out (bool bStatic, QsMetaItemId VarId)? outValue)
        {
            var candidates = new List<MemberInfo>();
            var memberInfos = type.GetMember(varName, MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if (1 < memberInfos.Length || !(memberInfos[0] is FieldInfo fieldInfo))
            {
                outValue = null;
                return false;
            }

            outValue = (fieldInfo.IsStatic, TypeId.Append(varName, 0));
            return true;
        }

        public override ImmutableArray<string> GetTypeParams()
        {
            return type.GetGenericArguments().Select(type => type.Name).ToImmutableArray();                
        }
    }

    class QsDotnetValue : QsValue
    {
        Object obj;
        FieldInfo fieldInfo;

        public QsDotnetValue(Object obj, FieldInfo fieldInfo)
        {
            this.obj = obj;
            this.fieldInfo = fieldInfo;
        }

        public override QsValue GetMemberValue(QsName varName)
        {
            throw new NotImplementedException();
        }

        public override QsTypeInst GetTypeInst()
        {
            throw new NotImplementedException();
        }

        public override QsValue MakeCopy()
        {
            return new QsDotnetValue(obj, fieldInfo);
        }

        public override void SetValue(QsValue fromValue)
        {
            if (fromValue is QsDotnetValue dotnetFromValue)
            {
                fieldInfo.SetValue(obj, dotnetFromValue.fieldInfo.GetValue(dotnetFromValue.obj));
            }
        }
    }

    // 
    class QsDotnetObject : QsObject
    {
        QsTypeInst typeInst;        
        Object obj;

        public QsDotnetObject(QsTypeInst typeInst, Object obj)
        {
            this.typeInst = typeInst;
            this.obj = obj;
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }

        public override QsValue GetMemberValue(QsName varName)
        {
            var fieldInfo = obj.GetType().GetField(varName.Name!);
            return new QsDotnetValue(obj, fieldInfo);
        }
    }

    // QsValue <- 사실은 Location 자체
    // QsObjectValue <- QsObject를 갖고 있다
    // 
    // QsIntValue = QsValueT<int>
    // 

    //public class QsDotnetModule : IQsModule
    //{
    //    Assembly assembly;
    //    public string ModuleName => assembly.FullName;

    //    public IEnumerable<QsType> Types { get; }
    //    public IEnumerable<QsFunc> Funcs => Enumerable.Empty<QsFunc>();
    //    public IEnumerable<QsVariable> Vars => Enumerable.Empty<QsVariable>();

    //    public IEnumerable<IQsModuleTypeInfo> TypeInfos => throw new NotImplementedException();
    //    public IEnumerable<IQsModuleFuncInfo> FuncInfos => Enumerable.Empty<IQsModuleFuncInfo>();

    //    public QsDotnetModule(Assembly assembly)
    //    {
    //        this.assembly = assembly;

    //        var types = new List<QsType>();
    //        foreach(var type in assembly.GetTypes())
    //        {
    //            types.Add(new QsDefaultType(typeId, );
    //        }

    //        Types = types;
    //    }

    //    string MakeDotnetName(QsMetaItemId typeId)
    //    {
    //        var sb = new StringBuilder();

    //        bool bFirst = true;
    //        foreach (var elem in typeId.Elems)
    //        {
    //            if (bFirst) bFirst = false;
    //            else sb.Append('.');

    //            sb.Append(elem.Name);

    //            if (elem.TypeParamCount != 0)
    //            {
    //                sb.Append('`');
    //                sb.Append(elem.TypeParamCount);
    //            }
    //        }

    //        return sb.ToString();
    //    }

    //    public bool GetTypeById(QsMetaItemId typeId, [NotNullWhen(true)] out QsType? outType)
    //    {
    //        var dotnetType = assembly.GetType(MakeDotnetName(typeId));

    //        if (dotnetType == null)
    //        {
    //            outType = null;
    //            return false;
    //        }

    //        outType = new QsDotnetType(typeId, dotnetType);
    //        return true;
    //    }

    //    public bool GetFuncById(QsMetaItemId funcId, [NotNullWhen(true)] out QsFunc? outFunc)
    //    {
    //        outFunc = null;
    //        return false;
    //    }

    //    public bool GetVarById(QsMetaItemId typeId, [NotNullWhen(true)] out QsVariable? outVar)
    //    {
    //        outVar = null;
    //        return false;
    //    }

    //    public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue fv)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal ntv)
    //    {
    //        if (!domainService.GetBaseTypeValue(ntv, out var baseTypeValue))
    //            throw new InvalidOperationException();

    //        QsTypeInst? baseTypeInst = null;
    //        if (baseTypeValue != null)
    //            baseTypeInst = domainService.GetTypeInst(baseTypeValue);

    //        var typeEnv = domainService.MakeTypeEnv(ntv);

    //        return new QsNativeTypeInst(baseTypeInst, ntv.TypeId, new QsObjectValue(null), typeEnv);
    //    }

    //    public void OnLoad(QsDomainService domainService)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
