﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

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

        public static QsType AddType(
            QsTypeBuilder typeBuilder,            
            QsTypeId enumeratorId,
            QsTypeValue intTypeValue)
        {
            QsTypeId listId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1));
            QsTypeValue listElemTypeValue = new QsTypeVarTypeValue(listId, "T");

            var memberFuncsBuilder = ImmutableDictionary.CreateBuilder<QsName, QsFuncId>();

            var listAdd = typeBuilder.AddFunc(NativeAdd, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem("Add", 0)),
                true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, listElemTypeValue));
            memberFuncsBuilder.Add(QsName.Text("Add"), listAdd.FuncId);

            var listRemoveAt = typeBuilder.AddFunc(NativeRemoveAt, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem("RemoveAt", 0)),
                true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, intTypeValue));
            memberFuncsBuilder.Add(QsName.Text("RemoveAt"), listRemoveAt.FuncId);

            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args) => NativeGetEnumerator(domainService, enumeratorId, typeArgs, thisValue, args);

            var listGetEnumerator = typeBuilder.AddFunc(wrappedGetEnumerator, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem("GetEnumerator", 0)),
                true, ImmutableArray<string>.Empty,
                new QsNormalTypeValue(null, enumeratorId, listElemTypeValue)));
            memberFuncsBuilder.Add(QsName.Text("GetEnumerator"), listGetEnumerator.FuncId);

            var listIndexer = typeBuilder.AddFunc(NativeIndexer, new QsFunc(
                new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem(QsName.Special(QsSpecialName.Indexer), 0)),
                true, ImmutableArray<string>.Empty,
                listElemTypeValue, intTypeValue));
            memberFuncsBuilder.Add(QsName.Special(QsSpecialName.Indexer), listIndexer.FuncId);

            var type = new QsDefaultType(
                listId, 
                ImmutableArray.Create("T"), // typeParams
                null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                memberFuncsBuilder.ToImmutable(),
                ImmutableDictionary<string, QsVarId>.Empty);

            return typeBuilder.AddType(type, new QsObjectValue(null));
        }
        
        // Enumerator<T> List<T>.GetEnumerator()
        static ValueTask<QsValue> NativeGetEnumerator(QsDomainService domainService, QsTypeId enumeratorId, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(thisValue != null);
            var list = GetObject<QsListObject>(thisValue);

            // enumerator<T>
            var enumeratorInst = domainService.GetTypeInst(new QsNormalTypeValue(null, enumeratorId, typeEnv.TypeValues[0]));

            // TODO: Runtime 메모리 관리자한테 new를 요청해야 합니다
            return new ValueTask<QsValue>(new QsObjectValue(new QsEnumeratorObject(enumeratorInst, ToAsyncEnum(list.Elems).GetAsyncEnumerator())));

#pragma warning disable CS1998
            async IAsyncEnumerable<QsValue> ToAsyncEnum(IEnumerable<QsValue> enumerable)
            {
                foreach(var elem in enumerable)
                    yield return elem;
            }
#pragma warning restore CS1998
        }

        static ValueTask<QsValue> NativeIndexer(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            return new ValueTask<QsValue>(list.Elems[((QsValue<int>)args[0]).Value]);
        }

        // List<T>.Add
        static ValueTask<QsValue> NativeAdd(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask<QsValue>(QsVoidValue.Instance);
        }

        static ValueTask<QsValue> NativeRemoveAt(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.RemoveAt(((QsValue<int>)args[0]).Value);
            
            return new ValueTask<QsValue>(QsVoidValue.Instance);
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
