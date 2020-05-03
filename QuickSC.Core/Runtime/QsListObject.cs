using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
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
                new QsObjectValue(new QsEnumeratorObject(list.Elems.GetEnumerator())), context));
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
