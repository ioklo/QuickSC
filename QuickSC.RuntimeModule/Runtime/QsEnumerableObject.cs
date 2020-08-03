using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsEnumerableObjectInfo : QsRuntimeModuleObjectInfo
    {
        public QsEnumerableObjectInfo()
            : base(null, QsRuntimeModule.EnumerableId, ImmutableArray.Create("T"), null, () => new QsObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleObjectBuilder builder)
        {            
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            // T
            var enumerableId = QsRuntimeModule.EnumerableId;
            var elemTypeValue = new QsTypeValue_TypeVar(enumerableId, "T");

            // Enumerator<T>
            var enumeratorTypeValue = new QsTypeValue_Normal(null, enumeratorId, elemTypeValue);

            var funcIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            Invoker wrappedGetEnumerator = 
                (domainService, typeArgs, thisValue, args, result) => QsEnumerableObject.NativeGetEnumerator(domainService, enumeratorId, typeArgs, thisValue, args, result);

            builder.AddMemberFunc(QsName.Text("GetEnumerator"),
                false, true, ImmutableArray<string>.Empty, enumeratorTypeValue, ImmutableArray<QsTypeValue>.Empty,
                wrappedGetEnumerator);
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
        internal static ValueTask NativeGetEnumerator(QsDomainService domainService, QsMetaItemId enumeratorId, QsTypeEnv typeEnv, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumerableObject = GetObject<QsEnumerableObject>(thisValue);

            var enumeratorInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, enumeratorId, typeEnv.TypeValues[0]));
            
            ((QsObjectValue)result).SetObject(new QsEnumeratorObject(enumeratorInst, enumerableObject.enumerable.GetAsyncEnumerator()));

            return new ValueTask();
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
