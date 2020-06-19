using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    class QsEnumerableObjectInfo : QsRuntimeModuleObjectInfo
    {
        public QsEnumerableObjectInfo()
        {
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            // T
            var enumerableId = QsRuntimeModule.EnumerableId;
            var elemTypeValue = new QsTypeVarTypeValue(enumerableId, "T");

            // Enumerator<T>
            var enumeratorTypeValue = new QsNormalTypeValue(null, enumeratorId, elemTypeValue);

            var funcIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            Invoker wrappedGetEnumerator = (domainService, typeArgs, thisValue, args) => QsEnumerableObject.NativeGetEnumerator(domainService, enumeratorId, typeArgs, thisValue, args);

            var nativeFunc = new QsNativeFunc(
                enumerableId.Append("GetEnumerator", 0),
                false, true, ImmutableArray<string>.Empty, enumeratorTypeValue, ImmutableArray<QsTypeValue>.Empty, new QsNativeFuncInstantiator(true, wrappedGetEnumerator));

            AddNativeFunc(nativeFunc);
            funcIdsBuilder.Add(nativeFunc.FuncId);

            AddNativeType(new QsNativeType(
                enumerableId, ImmutableArray.Create("T"), null,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                funcIdsBuilder.ToImmutable(),
                ImmutableArray<QsMetaItemId>.Empty, 
                new QsNativeTypeInstantiator(() => new QsObjectValue(null))));
        }
        
    }

    class QsEnumerableObject : QsObject
    {
        QsTypeInst typeInst;
        IAsyncEnumerable<QsValue> enumerable;

        public QsEnumerableObject(QsTypeInst typeInst, IAsyncEnumerable<QsValue> enumerable)
        {
            this.typeInst = typeInst;
            this.enumerable = enumerable;
        }
        
        // Enumerator<T> Enumerable<T>.GetEnumerator()
        internal static ValueTask<QsValue> NativeGetEnumerator(QsDomainService domainService, QsMetaItemId enumeratorId, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumerableObject = GetObject<QsEnumerableObject>(thisValue);

            var enumeratorInst = domainService.GetTypeInst(new QsNormalTypeValue(null, enumeratorId, typeEnv.TypeValues[0]));

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(new QsObjectValue(new QsEnumeratorObject(enumeratorInst, enumerableObject.enumerable.GetAsyncEnumerator())));
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
