using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsEnumeratorObjectInfo : QsRuntimeModuleObjectInfo
    {
        public QsEnumeratorObjectInfo()
            : base(null, QsRuntimeModule.EnumeratorId, ImmutableArray.Create("T"), null, () => new QsObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleObjectBuilder builder)
        {
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            var funcIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();
            var elemTypeValue = new QsTypeValue_TypeVar(enumeratorId, "T");
            var boolTypeValue = new QsTypeValue_Normal(null, QsRuntimeModule.BoolId);

            // bool Enumerator<T>.MoveNext()
            builder.AddMemberFunc(
                QsName.Text("MoveNext"),
                false, true,
                ImmutableArray<string>.Empty, boolTypeValue, ImmutableArray<QsTypeValue>.Empty,
                QsEnumeratorObject.NativeMoveNext);

            // T Enumerator<T>.GetCurrent()
            builder.AddMemberFunc(QsName.Text("GetCurrent"),
                false, true,
                ImmutableArray<string>.Empty, elemTypeValue, ImmutableArray<QsTypeValue>.Empty,
                QsEnumeratorObject.NativeGetCurrent);
        }
    }

    class QsEnumeratorObject : QsObject
    {
        QsTypeInst typeInst;
        IAsyncEnumerator<QsValue> enumerator;

        public QsEnumeratorObject(QsTypeInst typeInst, IAsyncEnumerator<QsValue> enumerator)
        {
            this.typeInst = typeInst;
            this.enumerator = enumerator;
        }

        internal static async ValueTask NativeMoveNext(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();

            ((QsValue<bool>)result).Value = bResult;
        }

        internal static ValueTask NativeGetCurrent(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);            
            result.SetValue(enumeratorObj.enumerator.Current);

            return new ValueTask();
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
