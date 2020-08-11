using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsListBuildInfo : QsRuntimeModuleTypeBuildInfo
    {
        public QsListBuildInfo()
            : base(null, QsRuntimeModule.ListId, ImmutableArray.Create("T"), null, () => new QsObjectValue(null))
        {
        }       

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            QsTypeValue intTypeValue = QsTypeValue.MakeNormal(QsRuntimeModule.IntId);
            QsTypeValue listElemTypeValue = QsTypeValue.MakeTypeVar(QsRuntimeModule.ListId, "T");            

            var memberFuncIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            // List<T>.Add
            builder.AddMemberFunc(QsName.MakeText("Add"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsTypeValue.MakeVoid(), ImmutableArray.Create(listElemTypeValue), QsListObject.NativeAdd);

            // List<T>.RemoveAt(int index)     
            builder.AddMemberFunc(QsName.MakeText("RemoveAt"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsTypeValue.MakeVoid(), ImmutableArray.Create(intTypeValue), QsListObject.NativeRemoveAt);

            // Enumerator<T> List<T>.GetEnumerator()
            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args, result) => QsListObject.NativeGetEnumerator(domainService, QsRuntimeModule.EnumeratorId, typeArgs, thisValue, args, result);

            builder.AddMemberFunc(QsName.MakeText("GetEnumerator"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsTypeValue.MakeNormal(QsRuntimeModule.EnumeratorId, QsTypeArgumentList.Make(listElemTypeValue)), ImmutableArray<QsTypeValue>.Empty, wrappedGetEnumerator);

            // T List<T>.Indexer(int index)
            builder.AddMemberFunc(QsSpecialNames.IndexerGet,
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
        internal static ValueTask NativeGetEnumerator(QsDomainService domainService, QsMetaItemId enumeratorId, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);
            var list = GetObject<QsListObject>(thisValue);

            // enumerator<T>
            var enumeratorInst = domainService.GetTypeInst(QsTypeValue.MakeNormal(enumeratorId, typeArgList));

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

        internal static ValueTask NativeIndexer(QsDomainService domainService, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Count == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            result!.SetValue(list.Elems[((QsValue<int>)args[0]).Value]);

            return new ValueTask();
        }

        // List<T>.Add
        internal static ValueTask NativeAdd(QsDomainService domainService, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Count == 1);
            Debug.Assert(thisValue != null);
            Debug.Assert(result == null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask();
        }

        internal static ValueTask NativeRemoveAt(QsDomainService domainService, QsTypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(args.Count == 1);
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
