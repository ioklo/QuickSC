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
            var typeId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("IEnumerator", 1));

            var funcIdsBuilder = ImmutableDictionary.CreateBuilder<QsName, QsFuncId>();
            var elemTypeValue = new QsTypeVarTypeValue(typeId, "T");

            // bool MoveNext()
            var moveNext = typeBuilder.AddFunc(NativeMoveNext, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("IEnumerator", 1), new QsNameElem("MoveNext", 0)),
                true, ImmutableArray<string>.Empty, boolTypeValue));
            funcIdsBuilder.Add(new QsName("MoveNext"), moveNext.FuncId);

            // T GetCurrent()
            var getCurrent = typeBuilder.AddFunc(NativeGetCurrent, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("IEnumerator", 1), new QsNameElem("GetCurrent", 0)),
                true, ImmutableArray<string>.Empty, elemTypeValue));
            funcIdsBuilder.Add(new QsName("GetCurrent"), getCurrent.FuncId);

            var type = new QsDefaultType(
                typeId,
                ImmutableArray.Create("T"),
                null, // no base
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                funcIdsBuilder.ToImmutable(),
                ImmutableDictionary<string, QsVarId>.Empty);

            return typeBuilder.AddType(type, new QsObjectValue(null));
        }

        public QsAsyncEnumeratorObject(IAsyncEnumerator<QsValue> enumerator)
        {
            this.enumerator = enumerator;
        }

        static async ValueTask<QsValue> NativeMoveNext(ImmutableArray<QsTypeInst> typeArgs, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);            

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();
            return new QsValue<bool>(bResult);
        }

        static ValueTask<QsValue> NativeGetCurrent(ImmutableArray<QsTypeInst> typeArgs, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);

            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);            

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue>(enumeratorObj.enumerator.Current);
        }
    }
}
