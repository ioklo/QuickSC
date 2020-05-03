using QuickSC.Syntax;
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

        static ValueTask<QsEvalResult<QsValue>> NativeMoveNext(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);
            if (enumeratorObj == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            bool bResult = enumeratorObj.enumerator.MoveNext();
            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsValue<bool>(bResult), context));
        }

        static ValueTask<QsEvalResult<QsValue>> NativeGetCurrent(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);
            if (enumeratorObj == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);            

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(enumeratorObj.enumerator.Current, context));
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
