using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    using Invoker = Func<DomainService, TypeArgumentList, Value?, IReadOnlyList<Value>, Value, ValueTask>;

    class QsListBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsListBuildInfo()
            : base(null, QsRuntimeModule.ListId, ImmutableArray.Create("T"), null, () => new ObjectValue(null))
        {
        }       

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            TypeValue intTypeValue = TypeValue.MakeNormal(QsRuntimeModule.IntId);
            TypeValue listElemTypeValue = TypeValue.MakeTypeVar(QsRuntimeModule.ListId, "T");            

            var memberFuncIdsBuilder = ImmutableArray.CreateBuilder<ModuleItemId>();

            // List<T>.Add
            builder.AddMemberFunc(Name.MakeText("Add"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                TypeValue.MakeVoid(), ImmutableArray.Create(listElemTypeValue), QsListObject.NativeAdd);

            // List<T>.RemoveAt(int index)     
            builder.AddMemberFunc(Name.MakeText("RemoveAt"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                TypeValue.MakeVoid(), ImmutableArray.Create(intTypeValue), QsListObject.NativeRemoveAt);

            // Enumerator<T> List<T>.GetEnumerator()
            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args, result) => QsListObject.NativeGetEnumerator(domainService, QsRuntimeModule.EnumeratorId, typeArgs, thisValue, args, result);

            builder.AddMemberFunc(Name.MakeText("GetEnumerator"),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                TypeValue.MakeNormal(QsRuntimeModule.EnumeratorId, TypeArgumentList.Make(listElemTypeValue)), ImmutableArray<TypeValue>.Empty, wrappedGetEnumerator);

            // T List<T>.Indexer(int index)
            builder.AddMemberFunc(SpecialNames.IndexerGet,
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                listElemTypeValue, ImmutableArray.Create(intTypeValue), QsListObject.NativeIndexer);
            
            return;
        }
    }

    // List
    public class QsListObject : GumObject
    {
        TypeInst typeInst;
        public List<Value> Elems { get; }

        public QsListObject(TypeInst typeInst, List<Value> elems)
        {
            this.typeInst = typeInst;
            Elems = elems;
        }
        
        // Enumerator<T> List<T>.GetEnumerator()
        internal static ValueTask NativeGetEnumerator(DomainService domainService, ModuleItemId enumeratorId, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);
            var list = GetObject<QsListObject>(thisValue);

            // enumerator<T>
            var enumeratorInst = domainService.GetTypeInst(TypeValue.MakeNormal(enumeratorId, typeArgList));

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            ((ObjectValue)result).SetObject(new QsEnumeratorObject(enumeratorInst, ToAsyncEnum(list.Elems).GetAsyncEnumerator()));

            return new ValueTask();

#pragma warning disable CS1998
            async IAsyncEnumerable<Value> ToAsyncEnum(IEnumerable<Value> enumerable)
            {
                foreach(var elem in enumerable)
                    yield return elem;
            }
#pragma warning restore CS1998
        }

        internal static ValueTask NativeIndexer(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(args.Count == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            result!.SetValue(list.Elems[((Value<int>)args[0]).Data]);

            return new ValueTask();
        }

        // List<T>.Add
        internal static ValueTask NativeAdd(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(args.Count == 1);
            Debug.Assert(thisValue != null);
            Debug.Assert(result == null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask();
        }

        internal static ValueTask NativeRemoveAt(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(args.Count == 1);
            Debug.Assert(thisValue != null);
            Debug.Assert(result == null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.RemoveAt(((Value<int>)args[0]).Data);

            return new ValueTask();
        }

        public override TypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
