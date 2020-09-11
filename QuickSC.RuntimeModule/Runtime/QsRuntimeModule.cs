using Gum.CompileTime;
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
        private ImmutableDictionary<ModuleItemId, ITypeInfo> typeInfos;
        private ImmutableDictionary<ModuleItemId, FuncInfo> funcInfos;
        private ImmutableDictionary<ModuleItemId, VarInfo> varInfos;
        private ImmutableDictionary<ModuleItemId, QsNativeTypeInstantiator> typeInstantiators;
        private ImmutableDictionary<ModuleItemId, QsNativeFuncInstantiator> funcInstantiators;

        private string homeDir;
        private string scriptDir;

        public const string MODULE_NAME = "System.Runtime";
        public string ModuleName => MODULE_NAME;

        public static ModuleItemId BoolId = ModuleItemId.Make("bool");
        public static ModuleItemId IntId = ModuleItemId.Make("int");
        public static ModuleItemId StringId = ModuleItemId.Make("string");
        public static ModuleItemId ListId = ModuleItemId.Make("List", 1);
        public static ModuleItemId EnumerableId = ModuleItemId.Make("Enumerable", 1);
        public static ModuleItemId EnumeratorId = ModuleItemId.Make("Enumerator", 1);

        public static ModuleItemId EnvironmentId = ModuleItemId.Make("Environment");
        public static ModuleItemId envId = ModuleItemId.Make("env");

        // TODO: localId와 globalId를 나눠야 할 것 같다. 내부에서는 LocalId를 쓰고, Runtime은 GlobalId로 구분해야 할 것 같다
        public bool GetTypeInfo(ModuleItemId id, [NotNullWhen(returnValue: true)] out ITypeInfo? outTypeInfo)
        {
            return typeInfos.TryGetValue(id, out outTypeInfo);
        }

        public bool GetFuncInfo(ModuleItemId id, [NotNullWhen(returnValue:true)] out FuncInfo? outFuncInfo)
        {
            return funcInfos.TryGetValue(id, out outFuncInfo);
        }

        public bool GetVarInfo(ModuleItemId id, [NotNullWhen(returnValue: true)] out VarInfo? outVarInfo)
        {
            return varInfos.TryGetValue(id, out outVarInfo);
        }
        
        public QsRuntimeModule(string homeDir, string scriptDir)
        {
            var envTypeId = ModuleItemId.Make("Environment", 0);

            var moduleBuilder = new QsRuntimeModuleBuilder();

            moduleBuilder.AddBuildInfo(new QsEmptyStructBuildInfo(BoolId, () => new QsValue<bool>(false)));
            moduleBuilder.AddBuildInfo(new QsIntBuildInfo(this));
            moduleBuilder.AddBuildInfo(new QsEmptyClassBuildInfo(StringId, () => new QsObjectValue(null)));
            moduleBuilder.AddBuildInfo(new QsEnumerableBuildInfo());
            moduleBuilder.AddBuildInfo(new QsEnumeratorBuildInfo());
            moduleBuilder.AddBuildInfo(new QsListBuildInfo());
            moduleBuilder.AddBuildInfo(new QsEnvironmentBuildInfo());

            // objectInfo를 돌면서
            moduleBuilder.AddVar(null, envId, false, TypeValue.MakeNormal(EnvironmentId));

            typeInfos = moduleBuilder.GetAllTypeInfos().ToImmutableDictionary(typeInfo => typeInfo.TypeId);
            funcInfos = moduleBuilder.GetAllFuncInfos().ToImmutableDictionary(funcInfo => funcInfo.FuncId);
            varInfos = moduleBuilder.GetAllVarInfos().ToImmutableDictionary(varInfo => varInfo.VarId);

            typeInstantiators = moduleBuilder.GetAllTypeInstantiators().ToImmutableDictionary(instantiator => instantiator.TypeId);
            funcInstantiators = moduleBuilder.GetAllFuncInstantiators().ToImmutableDictionary(instantiator => instantiator.FuncId);

            this.homeDir = homeDir;
            this.scriptDir = scriptDir;
        }

        class QsEmptyStructBuildInfo : QsRuntimeModuleTypeBuildInfo.Struct
        {
            public QsEmptyStructBuildInfo(ModuleItemId typeId, Func<QsValue> defaultValueFactory)
                : base(null, typeId, Enumerable.Empty<string>(), null, defaultValueFactory)
            {
            }

            public override void Build(QsRuntimeModuleTypeBuilder builder) 
            { 
            }
        }

        class QsEmptyClassBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
        {
            public QsEmptyClassBuildInfo(ModuleItemId typeId, Func<QsValue> defaultValueFactory)
                : base(null, typeId, Enumerable.Empty<string>(), null, defaultValueFactory)
            {
            }

            public override void Build(QsRuntimeModuleTypeBuilder builder)
            {
            }
        }

        public string GetString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringBuildInfo strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다
            throw new InvalidOperationException();
        }

        public void SetString(QsDomainService domainService, QsValue value, string s)
        {
            var stringInst = domainService.GetTypeInst(TypeValue.MakeNormal(StringId));
            ((QsObjectValue)value).SetObject(new QsStringBuildInfo(stringInst, s));
        }

        public void SetList(QsDomainService domainService, QsValue value, TypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(TypeValue.MakeNormal(ListId, TypeArgumentList.Make(elemTypeValue)));
            ((QsObjectValue)value).SetObject(new QsListObject(listInst, elems));
        }

        public void SetEnumerable(QsDomainService domainService, QsValue value, TypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            var enumerableInst = domainService.GetTypeInst(TypeValue.MakeNormal(EnumerableId, TypeArgumentList.Make(elemTypeValue)));
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
            var stringInst = domainService.GetTypeInst(TypeValue.MakeNormal(StringId));
            return new QsObjectValue(new QsStringBuildInfo(stringInst, str));
        }

        public QsValue MakeList(QsDomainService domainService, TypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(TypeValue.MakeNormal(ListId, TypeArgumentList.Make(elemTypeValue)));

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

            domainService.InitGlobalValue(envId, new QsObjectValue(new QsEnvironmentObject(homeDirValue, scriptDirValue)));
        }

        public QsObjectValue MakeNullObject()
        {
            return new QsObjectValue(null);
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, TypeValue.Normal ntv)
        {
            return typeInstantiators[ntv.TypeId].Instantiate(domainService, ntv);
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, FuncValue funcValue)
        {
            return funcInstantiators[funcValue.FuncId].Instantiate(domainService, funcValue);
        }
    }
}
