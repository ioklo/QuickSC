using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, QsValue, ValueTask>;

    class QsRuntimeModuleObjectBuilder
    {
        QsRuntimeModuleBuilder moduleBuilder;
        QsMetaItemId? outerTypeId;
        QsMetaItemId typeId;

        List<QsMetaItemId> memberTypeIds;
        List<QsMetaItemId> memberFuncIds;
        List<QsMetaItemId> memberVarIds;

        public static void BuildObject(QsRuntimeModuleBuilder runtimeModuleBuilder, QsRuntimeModuleObjectInfo objInfo)
        {
            var objectBuilder = new QsRuntimeModuleObjectBuilder(runtimeModuleBuilder, objInfo.GetOuterTypeId(), objInfo.GetId());

            objInfo.Build(objectBuilder);

            objectBuilder.BuildType(objInfo);
        }

        private QsRuntimeModuleObjectBuilder(QsRuntimeModuleBuilder moduleBuilder, QsMetaItemId? outerTypeId, QsMetaItemId typeId)
        {   
            this.moduleBuilder = moduleBuilder;
            this.outerTypeId = outerTypeId;
            this.typeId = typeId;

            memberTypeIds = new List<QsMetaItemId>();
            memberFuncIds = new List<QsMetaItemId>();
            memberVarIds = new List<QsMetaItemId>();
        }

        private void BuildType(QsRuntimeModuleObjectInfo objInfo)
        { 
            moduleBuilder.AddType(outerTypeId, typeId, objInfo.GetTypeParams(), objInfo.GetBaseTypeValue(), memberTypeIds, memberFuncIds, memberVarIds, objInfo.GetDefaultValueFactory());
        }

        public void AddMemberFunc(
            QsName funcName,
            bool bSeqCall, bool bThisCall,
            IReadOnlyList<string> typeParams,
            QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues,
            Invoker invoker)
        {
            var funcId = typeId.Append(new QsMetaItemIdElem(funcName, typeParams.Count));

            moduleBuilder.AddFunc(
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
            var varId = typeId.Append(new QsMetaItemIdElem(varName));

            moduleBuilder.AddVar(varId, bStatic, typeValue);

            memberVarIds.Add(varId);
        }
    }
}
