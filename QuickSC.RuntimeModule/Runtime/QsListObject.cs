using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsTypeInstEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    // List
    public class QsListObject : QsObject
    {
        public List<QsValue> Elems { get; }

        public QsListObject(List<QsValue> elems)
        {
            Elems = elems;
        }

        public static void AddType(
            QsTypeId enumeratorId,
            QsTypeValue intTypeValue,
            QsTypeBuilder typeBuilder)
        {
            typeBuilder.AddGlobalType(listId =>
            {
                QsTypeValue listElemTypeValue = new QsTypeVarTypeValue(listId, "T");

                var memberFuncsBuilder = ImmutableDictionary.CreateBuilder<QsFuncName, QsFuncId>();

                var listAdd = typeBuilder.AddFunc(NativeAdd, funcId => new QsFunc(
                    funcId, true, new QsFuncName("Add"), ImmutableArray<string>.Empty,
                    QsVoidTypeValue.Instance, listElemTypeValue));
                memberFuncsBuilder.Add(listAdd.Name, listAdd.FuncId);

                var listRemoveAt = typeBuilder.AddFunc(NativeRemoveAt, funcId => new QsFunc(
                    funcId, true, new QsFuncName("RemoveAt"), ImmutableArray<string>.Empty,
                    QsVoidTypeValue.Instance, intTypeValue));
                memberFuncsBuilder.Add(listRemoveAt.Name, listRemoveAt.FuncId);

                var listGetEnumerator = typeBuilder.AddFunc(NativeGetEnumerator, funcId => new QsFunc(
                    funcId, true, new QsFuncName("GetEnumerator"), ImmutableArray<string>.Empty,
                    new QsNormalTypeValue(null, enumeratorId, listElemTypeValue)));
                memberFuncsBuilder.Add(listGetEnumerator.Name, listGetEnumerator.FuncId);

                var listIndexer = typeBuilder.AddFunc(NativeIndexer, funcId => new QsFunc(
                    funcId, true, new QsFuncName(QsFuncNameKind.Indexer), ImmutableArray<string>.Empty,
                    listElemTypeValue, intTypeValue));                
                memberFuncsBuilder.Add(listIndexer.Name, listIndexer.FuncId);

                return new QsDefaultType(
                    listId,
                    "List",
                    ImmutableArray.Create("T"), // typeParams
                    null,
                    ImmutableDictionary<string, QsTypeId>.Empty,
                    ImmutableDictionary<string, QsFuncId>.Empty,
                    ImmutableDictionary<string, QsVarId>.Empty,
                    memberFuncsBuilder.ToImmutable(),
                    ImmutableDictionary<string, QsVarId>.Empty);
            });
        }
        
        static ValueTask<QsValue> NativeGetEnumerator(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);
            var list = GetObject<QsListObject>(thisValue);

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            return new ValueTask<QsValue>(new QsObjectValue(new QsAsyncEnumeratorObject(ToAsyncEnum(list.Elems).GetAsyncEnumerator())));

            async IAsyncEnumerable<QsValue> ToAsyncEnum(IEnumerable<QsValue> enumerable)
            {
                foreach(var elem in enumerable)
                    yield return elem;
            }
        }

        static ValueTask<QsValue> NativeIndexer(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length != 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            return new ValueTask<QsValue>(list.Elems[((QsValue<int>)args[0]).Value]);
        }

        // List<T>.Add
        static ValueTask<QsValue> NativeAdd(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length != 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask<QsValue>(QsVoidValue.Instance);
        }

        static ValueTask<QsValue> NativeRemoveAt(QsTypeInstEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length != 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.RemoveAt(((QsValue<int>)args[0]).Value);
            
            return new ValueTask<QsValue>(QsVoidValue.Instance);
        }
    }
}
