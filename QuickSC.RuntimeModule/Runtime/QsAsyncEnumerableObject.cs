using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    class QsAsyncEnumerableObject : QsObject
    {
        IAsyncEnumerable<QsValue> enumerable;

        public QsAsyncEnumerableObject(IAsyncEnumerable<QsValue> enumerable)
        {
            this.enumerable = enumerable;
        }
        
        static ValueTask<QsValue?> NativeGetEnumerator(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            var enumerableObject = GetObject<QsAsyncEnumerableObject>(thisValue);
            if (enumerableObject == null) return new ValueTask<QsValue?>((QsValue?)null);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsValue?>(new QsObjectValue(new QsAsyncEnumeratorObject(enumerableObject.enumerable.GetAsyncEnumerator())));
        }

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            if (funcId.Name == "GetEnumerator")
            {
                return new QsNativeCallable(NativeGetEnumerator);
            }
            
            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            return null;
        }
    }
}
