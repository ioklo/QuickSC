using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{   
    public class QsRuntimeModule : IQsRuntimeModule
    {
        private ImmutableDictionary<QsMetaItemId, QsTypeInfo> typeInfos;
        private ImmutableDictionary<QsMetaItemId, QsFuncInfo> funcInfos;
        private ImmutableDictionary<QsMetaItemId, QsVarInfo> varInfos;
        private ImmutableDictionary<QsMetaItemId, QsNativeTypeInstantiator> typeInstantiators;
        private ImmutableDictionary<QsMetaItemId, QsNativeFuncInstantiator> funcInstantiators;

        private string homeDir;
        private string scriptDir;

        public const string MODULE_NAME = "System.Runtime";
        public string ModuleName => MODULE_NAME;

        public static QsMetaItemId BoolId = new QsMetaItemId(new QsMetaItemIdElem("bool"));
        public static QsMetaItemId IntId = new QsMetaItemId(new QsMetaItemIdElem("int"));
        public static QsMetaItemId StringId = new QsMetaItemId(new QsMetaItemIdElem("string"));
        public static QsMetaItemId ListId = new QsMetaItemId(new QsMetaItemIdElem("List", 1));
        public static QsMetaItemId EnumerableId = new QsMetaItemId(new QsMetaItemIdElem("Enumerable", 1));
        public static QsMetaItemId EnumeratorId = new QsMetaItemId(new QsMetaItemIdElem("Enumerator", 1));

        public static QsMetaItemId EnvironmentId = new QsMetaItemId(new QsMetaItemIdElem("Environment"));
        public static QsMetaItemId envId = new QsMetaItemId(new QsMetaItemIdElem("env"));

        // TODO: localId와 globalId를 나눠야 할 것 같다. 내부에서는 LocalId를 쓰고, Runtime은 GlobalId로 구분해야 할 것 같다
        public bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(returnValue: true)] out QsTypeInfo? outTypeInfo)
        {
            return typeInfos.TryGetValue(id, out outTypeInfo);
        }

        public bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(returnValue:true)] out QsFuncInfo? outFuncInfo)
        {
            return funcInfos.TryGetValue(id, out outFuncInfo);
        }

        public bool GetVarInfo(QsMetaItemId id, [NotNullWhen(returnValue: true)] out QsVarInfo? outVarInfo)
        {
            return varInfos.TryGetValue(id, out outVarInfo);
        }
        
        public QsRuntimeModule(string homeDir, string scriptDir)
        {
            var envTypeId = new QsMetaItemId(new QsMetaItemIdElem("Environment", 0));

            var moduleBuilder = new QsRuntimeModuleBuilder();

            moduleBuilder.AddObjectInfo(new QsEmptyObjectInfo(BoolId, () => new QsValue<bool>(false)));
            moduleBuilder.AddObjectInfo(new QsIntValueInfo(this));
            moduleBuilder.AddObjectInfo(new QsEmptyObjectInfo(StringId, () => new QsObjectValue(null)));
            moduleBuilder.AddObjectInfo(new QsEnumerableObjectInfo());
            moduleBuilder.AddObjectInfo(new QsEnumeratorObjectInfo());
            moduleBuilder.AddObjectInfo(new QsListObjectInfo());
            moduleBuilder.AddObjectInfo(new QsEnvironmentInfo());

            // objectInfo를 돌면서
            moduleBuilder.AddVar(envId, false, new QsTypeValue_Normal(null, EnvironmentId));

            typeInfos = moduleBuilder.GetAllTypeInfos().ToImmutableDictionary(typeInfo => typeInfo.TypeId);
            funcInfos = moduleBuilder.GetAllFuncInfos().ToImmutableDictionary(funcInfo => funcInfo.FuncId);
            varInfos = moduleBuilder.GetAllVarInfos().ToImmutableDictionary(varInfo => varInfo.VarId);

            typeInstantiators = moduleBuilder.GetAllTypeInstantiators().ToImmutableDictionary(instantiator => instantiator.TypeId);
            funcInstantiators = moduleBuilder.GetAllFuncInstantiators().ToImmutableDictionary(instantiator => instantiator.FuncId);

            this.homeDir = homeDir;
            this.scriptDir = scriptDir;
        }

        class QsEmptyObjectInfo : QsRuntimeModuleObjectInfo
        {
            public QsEmptyObjectInfo(QsMetaItemId typeId, Func<QsValue> defaultValueFactory)
                : base(null, typeId, Enumerable.Empty<string>(), null, defaultValueFactory)
            {
            }

            public override void Build(QsRuntimeModuleObjectBuilder builder) 
            { 
            }
        }   

        public string GetString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다
            throw new InvalidOperationException();
        }

        public void SetString(QsDomainService domainService, QsValue value, string s)
        {
            var stringInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.StringId));
            ((QsObjectValue)value).SetObject(new QsStringObject(stringInst, s));
        }

        public void SetList(QsDomainService domainService, QsValue value, QsTypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.ListId, elemTypeValue));
            ((QsObjectValue)value).SetObject(new QsListObject(listInst, elems));
        }

        public void SetEnumerable(QsDomainService domainService, QsValue value, QsTypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            var enumerableInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.EnumerableId, elemTypeValue));
            ((QsObjectValue)value).SetObject(new QsEnumerableObject(enumerableInst, asyncEnumerable));
        }        
        
        public QsValue MakeBool(bool b)
        {
            return new QsValue<bool>(b);
        }

        public QsValue MakeInt(int i)
        {
            return new QsValue<int>(i);
        }

        public QsValue MakeString(QsDomainService domainService, string str)
        {
            var stringInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.StringId));
            return new QsObjectValue(new QsStringObject(stringInst, str));
        }

        public QsValue MakeList(QsDomainService domainService, QsTypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.ListId, elemTypeValue));

            return new QsObjectValue(new QsListObject(listInst, elems));
        }

        public int GetInt(QsValue value)
        {
            return ((QsValue<int>)value).Value;
        }
        
        public void SetInt(QsValue value, int i)
        {
            ((QsValue<int>)value).Value = i;
        }

        public bool GetBool(QsValue value)
        {
            return ((QsValue<bool>)value).Value;
        }

        public void SetBool(QsValue value, bool b)
        {
            ((QsValue<bool>)value).Value = b;
        }

        public void OnLoad(QsDomainService domainService)
        {
            // HomeDir?
            // ScriptDir?
            var homeDirValue = MakeString(domainService, homeDir);
            var scriptDirValue = MakeString(domainService, scriptDir);

            domainService.SetGlobalValue(envId, new QsObjectValue(new QsEnvironmentObject(homeDirValue, scriptDirValue)));
        }

        public QsObjectValue MakeNullObject()
        {
            return new QsObjectValue(null);
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal ntv)
        {
            return typeInstantiators[ntv.TypeId].Instantiate(domainService, ntv);
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            return funcInstantiators[funcValue.FuncId].Instantiate(domainService, funcValue);
        }
    }
}
