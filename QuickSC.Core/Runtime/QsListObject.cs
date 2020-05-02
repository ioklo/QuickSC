using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    public class QsListEnumeratorObject : QsObject
    {
        int i;
        List<QsValue> elems;

        public QsListEnumeratorObject(List<QsValue> elems)
        {
            this.elems = elems;
            i = -1;
        }

        static ValueTask<QsEvalResult<QsValue>> NativeMoveNext(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            var enumerator = GetObject<QsListEnumeratorObject>(thisValue);
            if (enumerator == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            if (enumerator.elems.Count <= enumerator.i + 1)
                return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsValue<bool>(false), context));

            enumerator.i++;
            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(new QsValue<bool>(true), context));
        }

        static ValueTask<QsEvalResult<QsValue>> NativeGetCurrent(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            var enumerator = GetObject<QsListEnumeratorObject>(thisValue);
            if (enumerator == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            if (enumerator.elems.Count <= enumerator.i)
                return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            // TODO: 여기 copy 해야 할 것 같음
            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(enumerator.elems[enumerator.i], context));
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

    // List
    public class QsListObject : QsObject
    {
        public List<QsValue> Elems { get; }

        public QsListObject(List<QsValue> elems)
        {
            Elems = elems;
        }

        static ValueTask<QsEvalResult<QsValue>> NativeGetEnumerator(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {   
            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(
                new QsObjectValue(new QsListEnumeratorObject(list.Elems)), context));
        }

        static ValueTask<QsEvalResult<QsValue>> NativeIndexer(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (args.Length != 1) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            if (args[0] is QsValue<int> index)
            {
                return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(list.Elems[index.Value], context));
            }
            else
            {
                return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);
            }
        }

        static ValueTask<QsEvalResult<QsValue>> NativeAdd(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (args.Length != 1) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            list.Elems.Add(args[0]);

            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(QsNullValue.Instance, context));
        }

        static ValueTask<QsEvalResult<QsValue>> NativeRemoveAt(QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (args.Length != 1) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);

            if (args[0] is QsValue<int> index)
            {
                list.Elems.RemoveAt(index.Value);
            }
            else
            {
                return new ValueTask<QsEvalResult<QsValue>>(QsEvalResult<QsValue>.Invalid);
            }

            return new ValueTask<QsEvalResult<QsValue>>(new QsEvalResult<QsValue>(QsNullValue.Instance, context));
        }

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            if (funcId.Kind == QsMemberFuncKind.Indexer)
            {
                return new QsNativeCallable(NativeIndexer);
            }
            else if (funcId.Name == "Add")
            {
                return new QsNativeCallable(NativeAdd);
            }
            else if (funcId.Name == "RemoveAt")
            {
                return new QsNativeCallable(NativeRemoveAt);
            }
            else if (funcId.Name == "GetEnumerator")
            {
                return new QsNativeCallable(NativeGetEnumerator);
            }

            return null;
        }

        public override QsValue GetMemberValue(string varName)
        {
            throw new NotImplementedException();
        }
    }

}
