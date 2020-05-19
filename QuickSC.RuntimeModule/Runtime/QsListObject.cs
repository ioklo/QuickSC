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
        
        static ValueTask<QsValue?> NativeGetEnumerator(QsValue thisValue, ImmutableArray<QsValue> args)
        {   
            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsValue?>((QsValue?)null);

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            return new ValueTask<QsValue?>(new QsObjectValue(new QsEnumeratorObject(list.Elems.GetEnumerator())));
        }

        static ValueTask<QsValue?> NativeIndexer(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            if (args.Length != 1) return new ValueTask<QsValue?>((QsValue?)null);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsValue?>((QsValue?)null);

            if (args[0] is QsValue<int> index)
            {
                return new ValueTask<QsValue?>(list.Elems[index.Value]);
            }
            else
            {
                return new ValueTask<QsValue?>((QsValue?)null);
            }
        }

        static ValueTask<QsValue?> NativeAdd(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            if (args.Length != 1) return new ValueTask<QsValue?>((QsValue?)null);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsValue?>((QsValue?)null);

            list.Elems.Add(args[0]);

            return new ValueTask<QsValue?>(QsNullValue.Instance);
        }

        static ValueTask<QsValue?> NativeRemoveAt(QsValue thisValue, ImmutableArray<QsValue> args)
        {
            if (args.Length != 1) return new ValueTask<QsValue?>((QsValue?)null);

            var list = GetObject<QsListObject>(thisValue);
            if (list == null) return new ValueTask<QsValue?>((QsValue?)null);

            if (args[0] is QsValue<int> index)
            {
                list.Elems.RemoveAt(index.Value);
            }
            else
            {
                return new ValueTask<QsValue?>((QsValue?)null);
            }

            return new ValueTask<QsValue?>(QsNullValue.Instance);
        }

        public override QsFuncInst GetMemberFuncInst(QsFuncId funcId)
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
