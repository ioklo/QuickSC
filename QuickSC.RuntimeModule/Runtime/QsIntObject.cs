using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsIntValueInfo : QsRuntimeModuleObjectInfo
    {
        QsRuntimeModule runtimeModule;

        public QsIntValueInfo(QsRuntimeModule runtimeModule)
            : base(null, QsRuntimeModule.IntId, ImmutableArray<string>.Empty, null, () => runtimeModule.MakeInt(0))
        {
            this.runtimeModule = runtimeModule;
        }

        public override void Build(QsRuntimeModuleObjectBuilder builder)
        {
            QsTypeValue intTypeValue = new QsTypeValue_Normal(null, QsRuntimeModule.IntId);

            builder.AddMemberFunc(QsName.Special(QsSpecialName.OpInc), false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorInc);
            builder.AddMemberFunc(QsName.Special(QsSpecialName.OpDec), false, false, ImmutableArray<string>.Empty, intTypeValue, ImmutableArray.Create(intTypeValue), OperatorDec);
        }

        ValueTask OperatorInc(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, IReadOnlyList<QsValue> argValues, QsValue resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue + 1);

            return default;
        }

        ValueTask OperatorDec(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, IReadOnlyList<QsValue> argValues, QsValue resultValue)
        {
            Debug.Assert(argValues.Count == 1);
            var source = argValues[0];

            int intValue = runtimeModule.GetInt(source);
            runtimeModule.SetInt(resultValue, intValue - 1);

            return default;
        }
    }
}
