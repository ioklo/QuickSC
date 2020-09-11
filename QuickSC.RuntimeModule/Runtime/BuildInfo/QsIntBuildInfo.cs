using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    class QsIntBuildInfo : QsRuntimeModuleTypeBuildInfo.Struct
    {
        QsRuntimeModule runtimeModule;

        public QsIntBuildInfo(QsRuntimeModule runtimeModule)
            : base(null, QsRuntimeModule.IntId, ImmutableArray<string>.Empty, null, () => runtimeModule.MakeInt(0))
        {
            this.runtimeModule = runtimeModule;
        }

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            TypeValue intTypeValue = TypeValue.MakeNormal(QsRuntimeModule.IntId);

            builder.AddMemberFunc(SpecialNames.OpInc, false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorInc);
            builder.AddMemberFunc(SpecialNames.OpDec, false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorDec);
        }

        ValueTask OperatorInc(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> argValues, Value resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue + 1);

            return default;
        }

        ValueTask OperatorDec(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> argValues, Value resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue - 1);

            return default;
        }
    }
}
