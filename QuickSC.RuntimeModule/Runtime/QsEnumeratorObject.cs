using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    public class QsEnumeratorObject : QsObject
    {
        IEnumerator<QsValue> enumerator;

        public QsEnumeratorObject(IEnumerator<QsValue> enumerator)
        {
            this.enumerator = enumerator;
        }

        static ValueTask<QsValue?> NativeMoveNext(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);
            if (enumeratorObj == null) return new ValueTask<QsValue?>((QsValue?)null);

            bool bResult = enumeratorObj.enumerator.MoveNext();
            return new ValueTask<QsValue?>(new QsValue<bool>(bResult));
        }

        static ValueTask<QsValue?> NativeGetCurrent(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);
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
