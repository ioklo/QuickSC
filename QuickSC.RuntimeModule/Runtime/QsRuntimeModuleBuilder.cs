using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, TypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsRuntimeModuleBuilder
    {
        List<ITypeInfo> typeInfos;
        List<FuncInfo> funcInfos;
        List<VarInfo> varInfos;

        List<QsNativeTypeInstantiator> typeInstantiators;
        List<QsNativeFuncInstantiator> funcInstantiators;

        public QsRuntimeModuleBuilder()
        {
            typeInfos = new List<ITypeInfo>();
            funcInfos = new List<FuncInfo>();
            varInfos = new List<VarInfo>();

            typeInstantiators = new List<QsNativeTypeInstantiator>();
            funcInstantiators = new List<QsNativeFuncInstantiator>();
        }

        public void AddBuildInfo(QsRuntimeModuleTypeBuildInfo buildInfo)
        {
            QsRuntimeModuleTypeBuilder.BuildObject(this, buildInfo);
        }

        public void AddClassType(
            ModuleItemId? outerTypeId,
            ModuleItemId typeId,
            IEnumerable<string> typeParams,
            TypeValue? baseTypeValue,
            IEnumerable<ModuleItemId> memberTypeIds,
            IEnumerable<ModuleItemId> memberFuncIds,
            IEnumerable<ModuleItemId> memberVarIds,
            Func<QsValue> defaultValueFactory)
        {
            typeInfos.Add(new ClassInfo(outerTypeId, typeId, typeParams, baseTypeValue, memberTypeIds, memberFuncIds, memberVarIds));
            typeInstantiators.Add(new QsNativeTypeInstantiator(typeId, defaultValueFactory));
        }

        public void AddStructType(
            ModuleItemId? outerTypeId,
            ModuleItemId typeId,
            IEnumerable<string> typeParams,
            TypeValue? baseTypeValue,
            IEnumerable<ModuleItemId> memberTypeIds,
            IEnumerable<ModuleItemId> memberFuncIds,
            IEnumerable<ModuleItemId> memberVarIds,
            Func<QsValue> defaultValueFactory)
        {
            typeInfos.Add(new StructInfo(outerTypeId, typeId, typeParams, baseTypeValue, memberTypeIds, memberFuncIds, memberVarIds));
            typeInstantiators.Add(new QsNativeTypeInstantiator(typeId, defaultValueFactory));
        }

        public void AddFunc(
            ModuleItemId? outerId,
            ModuleItemId funcId,
            bool bSeqCall,
            bool bThisCall,
            IReadOnlyList<string> typeParams,
            TypeValue retTypeValue,
            ImmutableArray<TypeValue> paramTypeValues,
            Invoker invoker)
        {
            funcInfos.Add(new FuncInfo(outerId, funcId, bSeqCall, bThisCall, typeParams, retTypeValue, paramTypeValues));
            funcInstantiators.Add(new QsNativeFuncInstantiator(funcId, bThisCall, invoker));
        }

        public void AddVar(ModuleItemId? outerId, ModuleItemId varId, bool bStatic, TypeValue typeValue)
        {
            varInfos.Add(new VarInfo(outerId, varId, bStatic, typeValue));
        }
        
        public IEnumerable<ITypeInfo> GetAllTypeInfos() => typeInfos;
        public IEnumerable<FuncInfo> GetAllFuncInfos() => funcInfos;
        public IEnumerable<VarInfo> GetAllVarInfos() => varInfos;
        public IEnumerable<QsNativeTypeInstantiator> GetAllTypeInstantiators() => typeInstantiators;
        public IEnumerable<QsNativeFuncInstantiator> GetAllFuncInstantiators() => funcInstantiators;
    }
}
