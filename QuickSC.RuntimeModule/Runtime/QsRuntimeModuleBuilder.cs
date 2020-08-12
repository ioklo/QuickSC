using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsRuntimeModuleBuilder
    {
        List<IQsTypeInfo> typeInfos;
        List<QsFuncInfo> funcInfos;
        List<QsVarInfo> varInfos;

        List<QsNativeTypeInstantiator> typeInstantiators;
        List<QsNativeFuncInstantiator> funcInstantiators;

        public QsRuntimeModuleBuilder()
        {
            typeInfos = new List<IQsTypeInfo>();
            funcInfos = new List<QsFuncInfo>();
            varInfos = new List<QsVarInfo>();

            typeInstantiators = new List<QsNativeTypeInstantiator>();
            funcInstantiators = new List<QsNativeFuncInstantiator>();
        }

        public void AddBuildInfo(QsRuntimeModuleTypeBuildInfo buildInfo)
        {
            QsRuntimeModuleTypeBuilder.BuildObject(this, buildInfo);
        }

        public void AddClassType(
            QsMetaItemId? outerTypeId,
            QsMetaItemId typeId,
            IEnumerable<string> typeParams,
            QsTypeValue? baseTypeValue,
            IEnumerable<QsMetaItemId> memberTypeIds,
            IEnumerable<QsMetaItemId> memberFuncIds,
            IEnumerable<QsMetaItemId> memberVarIds,
            Func<QsValue> defaultValueFactory)
        {
            typeInfos.Add(new QsClassInfo(outerTypeId, typeId, typeParams, baseTypeValue, memberTypeIds, memberFuncIds, memberVarIds));
            typeInstantiators.Add(new QsNativeTypeInstantiator(typeId, defaultValueFactory));
        }

        public void AddStructType(
            QsMetaItemId? outerTypeId,
            QsMetaItemId typeId,
            IEnumerable<string> typeParams,
            QsTypeValue? baseTypeValue,
            IEnumerable<QsMetaItemId> memberTypeIds,
            IEnumerable<QsMetaItemId> memberFuncIds,
            IEnumerable<QsMetaItemId> memberVarIds,
            Func<QsValue> defaultValueFactory)
        {
            typeInfos.Add(new QsStructInfo(outerTypeId, typeId, typeParams, baseTypeValue, memberTypeIds, memberFuncIds, memberVarIds));
            typeInstantiators.Add(new QsNativeTypeInstantiator(typeId, defaultValueFactory));
        }

        public void AddFunc(
            QsMetaItemId? outerId,
            QsMetaItemId funcId,
            bool bSeqCall,
            bool bThisCall,
            IReadOnlyList<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> paramTypeValues,
            Invoker invoker)
        {
            funcInfos.Add(new QsFuncInfo(outerId, funcId, bSeqCall, bThisCall, typeParams, retTypeValue, paramTypeValues));
            funcInstantiators.Add(new QsNativeFuncInstantiator(funcId, bThisCall, invoker));
        }

        public void AddVar(QsMetaItemId? outerId, QsMetaItemId varId, bool bStatic, QsTypeValue typeValue)
        {
            varInfos.Add(new QsVarInfo(outerId, varId, bStatic, typeValue));
        }
        
        public IEnumerable<IQsTypeInfo> GetAllTypeInfos() => typeInfos;
        public IEnumerable<QsFuncInfo> GetAllFuncInfos() => funcInfos;
        public IEnumerable<QsVarInfo> GetAllVarInfos() => varInfos;
        public IEnumerable<QsNativeTypeInstantiator> GetAllTypeInstantiators() => typeInstantiators;
        public IEnumerable<QsNativeFuncInstantiator> GetAllFuncInstantiators() => funcInstantiators;
    }
}
