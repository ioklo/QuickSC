using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<ImmutableArray<QsTypeInst>, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    class QsEnumerableObject : QsObject
    {
        QsTypeInst typeInst;
        IAsyncEnumerable<QsValue> enumerable;

        public static QsType AddType(QsTypeBuilder typeBuilder, QsRuntimeModule runtimeModule, QsTypeId enumeratorId)
        {
            // T
            var typeId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("Enumerable", 1));

            var elemTypeValue = new QsTypeVarTypeValue(typeId, "T");

            // Enumerator<T>
            var enumeratorTypeValue = new QsNormalTypeValue(null, enumeratorId, elemTypeValue);

            var funcBuilder = ImmutableDictionary.CreateBuilder<QsName, QsFuncId>();

            Invoker wrappedGetEnumerator = (typeArgs, thisValue, args) => NativeGetEnumerator(runtimeModule, enumeratorId, typeArgs, thisValue, args);

            var func = typeBuilder.AddFunc(wrappedGetEnumerator, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("Enumerable", 1), new QsNameElem("GetEnumerator", 0)),
                true, ImmutableArray<string>.Empty, enumeratorTypeValue));
            funcBuilder.Add(QsName.Text("GetEnumerator"), func.FuncId);

            var type = new QsDefaultType(
                typeId, ImmutableArray.Create("T"), null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                funcBuilder.ToImmutable(),
                ImmutableDictionary<string, QsVarId>.Empty);

            return typeBuilder.AddType(type, new QsObjectValue(null));
        }

        public QsEnumerableObject(QsTypeInst typeInst, IAsyncEnumerable<QsValue> enumerable)
        {
            this.typeInst = typeInst;
            this.enumerable = enumerable;
        }
        
        // Enumerator<T> Enumerable<T>.GetEnumerator()
        static ValueTask<QsValue> NativeGetEnumerator(QsRuntimeModule runtimeModule, QsTypeId enumeratorId, ImmutableArray<QsTypeInst> typeArgs, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumerableObject = GetObject<QsEnumerableObject>(thisValue);

            var enumeratorInst = runtimeModule.GetTypeInst(enumeratorId, typeArgs);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(new QsObjectValue(new QsEnumeratorObject(enumeratorInst, enumerableObject.enumerable.GetAsyncEnumerator())));
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
