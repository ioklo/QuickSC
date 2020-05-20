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
            typeBuilder.AddGlobalType(typeId =>
            {
                // T
                var elemTypeValue = new QsTypeVarTypeValue(typeId, "T");

                // IEnumerable<T>
                var ienumeratorTypeValue = new QsNormalTypeValue(null, ienumeratorId, elemTypeValue);

                var funcBuilder = ImmutableDictionary.CreateBuilder<QsFuncName, QsFuncId>();
                var func = typeBuilder.AddFunc(NativeGetEnumerator, funcId => new QsFunc(funcId, true, new QsFuncName("GetEnumerator"), ImmutableArray<string>.Empty, ienumeratorTypeValue));
                funcBuilder.Add(func.Name, func.FuncId);

                return new QsDefaultType(
                    typeId, "IEnumerable", ImmutableArray.Create("T"), null,
                    ImmutableDictionary<string, QsTypeId>.Empty,
                    ImmutableDictionary<string, QsFuncId>.Empty,
                    ImmutableDictionary<string, QsVarId>.Empty,
                    funcBuilder.ToImmutable(),
                    ImmutableDictionary<string, QsVarId>.Empty);
            });
        }

        public QsAsyncEnumerableObject(IAsyncEnumerable<QsValue> enumerable)
        {
            this.enumerable = enumerable;
        }
        
        static ValueTask<QsValue> NativeGetEnumerator(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumerableObject = GetObject<QsAsyncEnumerableObject>(thisValue);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(new QsObjectValue(new QsAsyncEnumeratorObject(enumerableObject.enumerable.GetAsyncEnumerator())));
        }
    }
}
