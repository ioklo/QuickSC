using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, QsValue, ValueTask>;

    class QsListObjectInfo : QsRuntimeModuleObjectInfo
    {
        public QsListObjectInfo()
            : base(null, QsRuntimeModule.ListId, ImmutableArray.Create("T"), null, () => new QsObjectValue(null))
        {
        }       

        public override void Build(QsRuntimeModuleObjectBuilder builder)
        {
            QsTypeValue intTypeValue = new QsTypeValue_Normal(null, QsRuntimeModule.IntId);
            QsTypeValue listElemTypeValue = new QsTypeValue_TypeVar(QsRuntimeModule.ListId, "T");

            var memberFuncIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            // List<T>.Add
            builder.AddMemberFunc(QsName.Text("Add"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsTypeValue_Void.Instance, ImmutableArray.Create(listElemTypeValue), QsListObject.NativeAdd);

            // List<T>.RemoveAt(int index)     
            builder.AddMemberFunc(QsName.Text("RemoveAt"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsTypeValue_Void.Instance, ImmutableArray.Create(intTypeValue), QsListObject.NativeRemoveAt);

            // Enumerator<T> List<T>.GetEnumerator()
            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args, result) => QsListObject.NativeGetEnumerator(domainService, QsRuntimeModule.EnumeratorId, typeArgs, thisValue, args, result);

            builder.AddMemberFunc(QsName.Text("GetEnumerator"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                new QsTypeValue_Normal(null, QsRuntimeModule.EnumeratorId, listElemTypeValue), ImmutableArray<QsTypeValue>.Empty, wrappedGetEnumerator);

            // T List<T>.Indexer(int index)
            builder.AddMemberFunc(QsName.Special(QsSpecialName.Indexer),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                listElemTypeValue, ImmutableArray.Create(intTypeValue), QsListObject.NativeIndexer);
            
            return;
        }
    }

    // List
    public class QsListObject : QsObject
    {
        QsTypeInst typeInst;
        public List<QsValue> Elems { get; }

        public QsListObject(QsTypeInst typeInst, List<QsValue> elems)
        {
            this.typeInst = typeInst;
            Elems = elems;
        }
        
        // Enumerator<T> List<T>.GetEnumerator()
        internal static ValueTask NativeGetEnumerator(QsDomainService domainService, QsMetaItemId enumeratorId, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);
            var list = GetObject<QsListObject>(thisValue);

            // enumerator<T>
            var enumeratorInst = domainService.GetTypeInst(new QsTypeValue_Normal(null, enumeratorId, typeEnv.TypeValues[0]));

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            ((QsObjectValue)result).SetObject(new QsEnumeratorObject(enumeratorInst, ToAsyncEnum(list.Elems).GetAsyncEnumerator()));

            return new ValueTask();

#pragma warning disable CS1998
            async IAsyncEnumerable<QsValue> ToAsyncEnum(IEnumerable<QsValue> enumerable)
            {
                foreach(var elem in enumerable)
                    yield return elem;
            }
#pragma warning restore CS1998
        }

        internal static ValueTask NativeIndexer(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            result!.SetValue(list.Elems[((QsValue<int>)args[0]).Value]);

            return new ValueTask();
        }

        // List<T>.Add
        internal static ValueTask NativeAdd(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);
            Debug.Assert(result == null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask();
        }

        internal static ValueTask NativeRemoveAt(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);
            Debug.Assert(result == null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.RemoveAt(((QsValue<int>)args[0]).Value);

            return new ValueTask();
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
