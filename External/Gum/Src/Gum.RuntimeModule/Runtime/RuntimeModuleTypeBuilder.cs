﻿using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Invoker = Gum.Runtime.RuntimeModuleMisc.Invoker;

namespace Gum.Runtime
{
    class RuntimeModuleTypeBuilder
    {
        RuntimeModuleBuilder moduleBuilder;
        ModuleItemId? outerTypeId;
        ModuleItemId typeId;

        List<ModuleItemId> memberTypeIds;
        List<ModuleItemId> memberFuncIds;
        List<ModuleItemId> memberVarIds;

        public static void BuildObject(RuntimeModuleBuilder runtimeModuleBuilder, RuntimeModuleTypeBuildInfo buildInfo)
        {
            var objectBuilder = new RuntimeModuleTypeBuilder(runtimeModuleBuilder, buildInfo.GetOuterTypeId(), buildInfo.GetId());

            buildInfo.Build(objectBuilder);

            objectBuilder.BuildType(buildInfo);
        }

        private RuntimeModuleTypeBuilder(RuntimeModuleBuilder moduleBuilder, ModuleItemId? outerTypeId, ModuleItemId typeId)
        {   
            this.moduleBuilder = moduleBuilder;
            this.outerTypeId = outerTypeId;
            this.typeId = typeId;

            memberTypeIds = new List<ModuleItemId>();
            memberFuncIds = new List<ModuleItemId>();
            memberVarIds = new List<ModuleItemId>();
        }

        private void BuildType(RuntimeModuleTypeBuildInfo buildInfo)
        {
            if (buildInfo is RuntimeModuleTypeBuildInfo.Class classBuildInfo)
                moduleBuilder.AddClassType(outerTypeId, typeId, classBuildInfo.GetTypeParams(), classBuildInfo.GetBaseTypeValue(), memberTypeIds, memberFuncIds, memberVarIds, classBuildInfo.GetDefaultValueFactory());
            else if (buildInfo is RuntimeModuleTypeBuildInfo.Struct structBuildInfo)
                moduleBuilder.AddStructType(outerTypeId, typeId, structBuildInfo.GetTypeParams(), structBuildInfo.GetBaseTypeValue(), memberTypeIds, memberFuncIds, memberVarIds, structBuildInfo.GetDefaultValueFactory());
            else
                throw new InvalidOperationException();
        }

        public void AddMemberFunc(
            Name funcName,
            bool bSeqCall, bool bThisCall,
            IReadOnlyList<string> typeParams,
            TypeValue retTypeValue, ImmutableArray<TypeValue> paramTypeValues,
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

        public void AddMemberVar(Name varName, bool bStatic, TypeValue typeValue)
        {
            var varId = typeId.Append(varName);

            moduleBuilder.AddVar(typeId, varId, bStatic, typeValue);

            memberVarIds.Add(varId);
        }
    }
}
