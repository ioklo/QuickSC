using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    abstract class QsRuntimeModuleTypeBuildInfo
    {
        ModuleItemId? outerTypeId;
        ModuleItemId id;
        IEnumerable<string> typeParams;
        TypeValue? baseTypeValue;
        Func<Value> defaultValueFactory;

        public ModuleItemId? GetOuterTypeId() => outerTypeId;
        public ModuleItemId GetId() => id;
        public IEnumerable<string> GetTypeParams() => typeParams;
        public TypeValue? GetBaseTypeValue() => baseTypeValue;
        public Func<Value> GetDefaultValueFactory() => defaultValueFactory;

        public QsRuntimeModuleTypeBuildInfo(ModuleItemId? outerTypeId, ModuleItemId id, IEnumerable<string> typeParams, TypeValue? baseTypeValue, Func<Value> defaultValueFactory)
        {
            this.outerTypeId = outerTypeId;
            this.id = id;
            this.typeParams = typeParams;
            this.baseTypeValue = baseTypeValue;
            this.defaultValueFactory = defaultValueFactory;
        }

        public abstract class Class : QsRuntimeModuleTypeBuildInfo
        {   
            public Class(ModuleItemId? outerTypeId, ModuleItemId id, IEnumerable<string> typeParams, TypeValue? baseTypeValue, Func<Value> defaultValueFactory)
                : base(outerTypeId, id, typeParams, baseTypeValue, defaultValueFactory)
            {   
            }
        }

        public abstract class Struct : QsRuntimeModuleTypeBuildInfo
        {
            public Struct(ModuleItemId? outerTypeId, ModuleItemId id, IEnumerable<string> typeParams, TypeValue? baseTypeValue, Func<Value> defaultValueFactory)
                : base(outerTypeId, id, typeParams, baseTypeValue, defaultValueFactory)
            {
            }
        }

        public abstract void Build(QsRuntimeModuleTypeBuilder builder);
    }
}
