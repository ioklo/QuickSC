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
    public class QsNativeModuleTypeInfo : IQsModuleTypeInfo
    {
        public QsMetaItemId TypeId { get; }
        private QsNativeTypeInstantiator instantiator;
        
        public QsNativeModuleTypeInfo(QsMetaItemId typeId, QsNativeTypeInstantiator instantiator)
        {
            TypeId = typeId;
            this.instantiator = instantiator;
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal typeValue)
            => instantiator.Instantiate(domainService, typeValue);
    }

    public class QsNativeModuleFuncInfo : IQsModuleFuncInfo
    {
        public QsMetaItemId FuncId { get; }
        private QsNativeFuncInstantiator instantiator;

        public QsNativeModuleFuncInfo(QsMetaItemId funcId, QsNativeFuncInstantiator instantiator)
        {
            FuncId = funcId;
            this.instantiator = instantiator;
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
            => instantiator.Instantiate(domainService, funcValue);
    }
    
    public class QsRuntimeModule : IQsRuntimeModule
    {
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
        public QsVariable EnvVar { get; }

        public IEnumerable<QsType> Types { get; }
        public IEnumerable<QsFunc> Funcs { get; }
        public IEnumerable<QsVariable> Vars { get; }

        public IEnumerable<IQsModuleTypeInfo> TypeInfos { get; }
        public IEnumerable<IQsModuleFuncInfo> FuncInfos { get; }

        string homeDir;
        string scriptDir;

        public QsRuntimeModule(string homeDir, string scriptDir)
        {
            var envTypeId = new QsMetaItemId(new QsMetaItemIdElem("Environment", 0));

            var objInfos = new List<IQsRuntimeModuleObjectInfo>();
            objInfos.Add(new QsEmptyObjectInfo(BoolId, () => new QsValue<bool>(false)));
            objInfos.Add(new QsEmptyObjectInfo(IntId, () => new QsValue<int>(0)));
            objInfos.Add(new QsEmptyObjectInfo(StringId, () => new QsObjectValue(null)));
            objInfos.Add(new QsEnumerableObjectInfo());
            objInfos.Add(new QsEnumeratorObjectInfo());
            objInfos.Add(new QsListObjectInfo());
            objInfos.Add(new QsEnvironmentInfo());

            EnvVar = new QsVariable(false, envId, new QsTypeValue_Normal(null, envTypeId));
            
            // objectInfo를 돌면서
            var moduleBuilder = new QsRuntimeModuleBuilder();
            moduleBuilder.AddVar(envId, new QsTypeValue_Normal(null, EnvironmentId));

            foreach (var objInfo in objInfos)
                objInfo.BuildModule(moduleBuilder);

            Types = moduleBuilder.GetTypes();
            Funcs = moduleBuilder.GetFuncs();
            Vars = moduleBuilder.GetVars();

            TypeInfos = moduleBuilder.ModuleTypeInfosBuilder.ToImmutable();
            FuncInfos = moduleBuilder.ModuleFuncInfosBuilder.ToImmutable();

            this.homeDir = homeDir;
            this.scriptDir = scriptDir;
        }

        class QsEmptyObjectInfo : IQsRuntimeModuleObjectInfo
        {
            private QsMetaItemId typeId;
            private Func<QsValue> defaultValueFactory;

            public QsEmptyObjectInfo(QsMetaItemId typeId, Func<QsValue> defaultValueFactory)
            {
                this.typeId = typeId;
                this.defaultValueFactory = defaultValueFactory;
            }
            
            public void BuildModule(QsRuntimeModuleBuilder builder)
            {
                builder.AddType(
                    typeId,
                    ImmutableArray<string>.Empty,
                    null,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty);

                builder.AddTypeInstantiator(typeId, new QsNativeTypeInstantiator(defaultValueFactory));
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

        public QsValue MakeEnumerable(QsDomainService domainService, QsTypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            var enumerableInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, QsRuntimeModule.EnumerableId, elemTypeValue));
            return new QsObjectValue(new QsEnumerableObject(enumerableInst, asyncEnumerable));
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
    }
}
