using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsAsyncEnumerableObject : QsObject
    {
        IAsyncEnumerable<QsValue> enumerable;

        public static void AddType(QsTypeBuilder typeBuilder, QsTypeId ienumeratorId)
        {
            // T
            var typeId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("IEnumerable", 1));

            var elemTypeValue = new QsTypeVarTypeValue(typeId, "T");

            // IEnumerable<T>
            var ienumeratorTypeValue = new QsNormalTypeValue(null, ienumeratorId, elemTypeValue);

            var funcBuilder = ImmutableDictionary.CreateBuilder<QsName, QsFuncId>();
            var func = typeBuilder.AddFunc(NativeGetEnumerator, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem("GetEnumerator", 0)),
                true, ImmutableArray<string>.Empty, ienumeratorTypeValue));
            funcBuilder.Add(QsName.Text("GetEnumerator"), func.FuncId);

            var type = new QsDefaultType(
                typeId, ImmutableArray.Create("T"), null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                funcBuilder.ToImmutable(),
                ImmutableDictionary<string, QsVarId>.Empty);

            typeBuilder.AddType(type, new QsObjectValue(null));
        }

        public QsAsyncEnumerableObject(IAsyncEnumerable<QsValue> enumerable)
        {
            this.enumerable = enumerable;
        }
        
        static ValueTask<QsValue> NativeGetEnumerator(ImmutableArray<QsTypeInst> typeArgs, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumerableObject = GetObject<QsAsyncEnumerableObject>(thisValue);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(new QsObjectValue(new QsAsyncEnumeratorObject(enumerableObject.enumerable.GetAsyncEnumerator())));
        }
    }
}
