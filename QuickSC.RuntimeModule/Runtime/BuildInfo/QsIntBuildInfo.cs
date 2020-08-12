using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
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
            QsTypeValue intTypeValue = QsTypeValue.MakeNormal(QsRuntimeModule.IntId);

            builder.AddMemberFunc(QsSpecialNames.OpInc, false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorInc);
            builder.AddMemberFunc(QsSpecialNames.OpDec, false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorDec);
        }

        ValueTask OperatorInc(QsDomainService domainService, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> argValues, QsValue resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue + 1);

            return default;
        }

        ValueTask OperatorDec(QsDomainService domainService, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> argValues, QsValue resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue - 1);

            return default;
        }
    }
}
