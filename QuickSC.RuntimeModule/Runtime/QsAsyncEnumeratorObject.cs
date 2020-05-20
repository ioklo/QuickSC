using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsAsyncEnumeratorObject : QsObject
    {
        IAsyncEnumerator<QsValue> enumerator;

        public static QsType AddType(QsTypeBuilder typeBuilder, QsTypeValue boolTypeValue)
        {
            return typeBuilder.AddGlobalType(typeId =>
            {
                var funcIdsBuilder = ImmutableDictionary.CreateBuilder<QsFuncName, QsFuncId>();
                var elemTypeValue = new QsTypeVarTypeValue(typeId, "T");

                // bool MoveNext()
                var moveNext = typeBuilder.AddFunc(NativeMoveNext, funcId => new QsFunc(funcId, true, new QsFuncName("MoveNext"), ImmutableArray<string>.Empty, boolTypeValue));
                funcIdsBuilder.Add(moveNext.Name, moveNext.FuncId);

                // T GetCurrent()
                var getCurrent = typeBuilder.AddFunc(NativeGetCurrent, funcId => new QsFunc(funcId, true, new QsFuncName("GetCurrent"), ImmutableArray<string>.Empty, elemTypeValue));
                funcIdsBuilder.Add(getCurrent.Name, getCurrent.FuncId);

                return new QsDefaultType(
                    typeId,
                    "IEnumerator",
                    ImmutableArray.Create("T"),
                    null, // no base
                    ImmutableDictionary<string, QsTypeId>.Empty,
                    ImmutableDictionary<string, QsFuncId>.Empty,
                    ImmutableDictionary<string, QsVarId>.Empty,
                    funcIdsBuilder.ToImmutable(),
                    ImmutableDictionary<string, QsVarId>.Empty);
            });
        }

        public QsAsyncEnumeratorObject(IAsyncEnumerator<QsValue> enumerator)
        {
            this.enumerator = enumerator;
        }

        static async ValueTask<QsValue> NativeMoveNext(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);            

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();
            return new QsValue<bool>(bResult);
        }

        static ValueTask<QsValue> NativeGetCurrent(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);            

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(enumeratorObj.enumerator.Current);
        }
    }
}
