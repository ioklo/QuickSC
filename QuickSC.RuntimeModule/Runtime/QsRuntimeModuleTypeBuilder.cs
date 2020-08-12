using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsRuntimeModuleTypeBuilder
    {
        QsRuntimeModuleBuilder moduleBuilder;
        QsMetaItemId? outerTypeId;
        QsMetaItemId typeId;

        List<QsMetaItemId> memberTypeIds;
        List<QsMetaItemId> memberFuncIds;
        List<QsMetaItemId> memberVarIds;

        public static void BuildObject(QsRuntimeModuleBuilder runtimeModuleBuilder, QsRuntimeModuleTypeBuildInfo buildInfo)
        {
            var objectBuilder = new QsRuntimeModuleTypeBuilder(runtimeModuleBuilder, buildInfo.GetOuterTypeId(), buildInfo.GetId());

            buildInfo.Build(objectBuilder);

            objectBuilder.BuildType(buildInfo);
        }

        private QsRuntimeModuleTypeBuilder(QsRuntimeModuleBuilder moduleBuilder, QsMetaItemId? outerTypeId, QsMetaItemId typeId)
        {   
            this.moduleBuilder = moduleBuilder;
            this.outerTypeId = outerTypeId;
            this.typeId = typeId;

            memberTypeIds = new List<QsMetaItemId>();
            memberFuncIds = new List<QsMetaItemId>();
            memberVarIds = new List<QsMetaItemId>();
        }

        private void BuildType(QsRuntimeModuleTypeBuildInfo buildInfo)
        {
            if (buildInfo is QsRuntimeModuleTypeBuildInfo.Class classBuildInfo)
                moduleBuilder.AddClassType(outerTypeId, typeId, classBuildInfo.GetTypeParams(), classBuildInfo.GetBaseTypeValue(), memberTypeIds, memberFuncIds, memberVarIds, classBuildInfo.GetDefaultValueFactory());
            else if (buildInfo is QsRuntimeModuleTypeBuildInfo.Struct structBuildInfo)
                moduleBuilder.AddStructType(outerTypeId, typeId, structBuildInfo.GetTypeParams(), structBuildInfo.GetBaseTypeValue(), memberTypeIds, memberFuncIds, memberVarIds, structBuildInfo.GetDefaultValueFactory());
            else
                throw new InvalidOperationException();
        }

        public void AddMemberFunc(
            QsName funcName,
            bool bSeqCall, bool bThisCall,
            IReadOnlyList<string> typeParams,
            QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues,
            Invoker invoker)
        {
            var funcId = typeId.Append(funcName, typeParams.Count);

            moduleBuilder.AddFunc(
                typeId,
                funcId,
                bSeqCall,
                bThisCall,
                typeParams,
                retTypeValue,
                paramTypeValues,
                invoker);

            memberFuncIds.Add(funcId);
        }

        public void AddMemberVar(QsName varName, bool bStatic, QsTypeValue typeValue)
        {
            var varId = typeId.Append(varName);

            moduleBuilder.AddVar(typeId, varId, bStatic, typeValue);

            memberVarIds.Add(varId);
        }
    }
}
