using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsAsyncEnumeratorObject : QsObject
    {
        IAsyncEnumerator<QsValue> enumerator;

        public QsAsyncEnumeratorObject(IAsyncEnumerator<QsValue> enumerator)
        {
            this.enumerator = enumerator;
        }

        static async ValueTask<QsValue?> NativeMoveNext(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);
            if (enumeratorObj == null) return null;

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();
            return new QsValue<bool>(bResult);
        }

        static ValueTask<QsValue?> NativeGetCurrent(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            var enumeratorObj = GetObject<QsAsyncEnumeratorObject>(thisValue);
            if (enumeratorObj == null) return new ValueTask<QsValue?>((QsValue?)null);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue?>(enumeratorObj.enumerator.Current);
        }

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            if (funcId.Name == "MoveNext")
            {
                return new QsNativeCallable(NativeMoveNext);
            }
            else if (funcId.Name == "GetCurrent")
            {
                return new QsNativeCallable(NativeGetCurrent);
            }

            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            return null;
        }
    }
}
