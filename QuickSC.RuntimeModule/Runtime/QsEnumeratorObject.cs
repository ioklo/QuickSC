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
        {
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            var funcIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();
            var elemTypeValue = new QsTypeValue_TypeVar(enumeratorId, "T");
            var boolTypeValue = new QsTypeValue_Normal(null, QsRuntimeModule.BoolId);

            // bool Enumerator<T>.MoveNext()
            var moveNext = new QsNativeFunc(
                enumeratorId.Append("MoveNext", 0),
                false, true,
                ImmutableArray<string>.Empty, boolTypeValue, ImmutableArray<QsTypeValue>.Empty,
                new QsNativeFuncInstantiator(true, QsEnumeratorObject.NativeMoveNext));

            AddNativeFunc(moveNext);
            funcIdsBuilder.Add(moveNext.FuncId);

            // T Enumerator<T>.GetCurrent()
            var getCurrent = new QsNativeFunc(
                enumeratorId.Append("GetCurrent", 0),
                false, true,
                ImmutableArray<string>.Empty, elemTypeValue, ImmutableArray<QsTypeValue>.Empty,
                new QsNativeFuncInstantiator(true, QsEnumeratorObject.NativeGetCurrent));
            AddNativeFunc(getCurrent);
            funcIdsBuilder.Add(getCurrent.FuncId);

            var type = new QsNativeType(
                enumeratorId,
                ImmutableArray.Create("T"),
                null, // no base
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                funcIdsBuilder.ToImmutable(),
                ImmutableArray<QsMetaItemId>.Empty,
                new QsNativeTypeInstantiator(() => new QsObjectValue(null)));

            AddNativeType(type);
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

        internal static async ValueTask<QsValue> NativeMoveNext(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);            

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();
            return new QsValue<bool>(bResult);
        }

        internal static ValueTask<QsValue> NativeGetCurrent(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);            

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(enumeratorObj.enumerator.Current);
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
