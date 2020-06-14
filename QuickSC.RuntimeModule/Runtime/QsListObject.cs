using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    // RuntimeModule은 NativeObjectInfo를 들고 있다가, Metadata가 필요하면 metadata를, Inst정보가 필요하면 inst정보를.. 인데.. 
    public class QsNativeMetaBuilder
    {
        string moduleName;
        ImmutableArray<QsType>.Builder typesBuilder;
        ImmutableArray<QsFunc>.Builder funcsBuilder;

        public QsNativeMetaBuilder(string moduleName)
        {
            this.moduleName = moduleName;
            typesBuilder = ImmutableArray.CreateBuilder<QsType>();
            funcsBuilder = ImmutableArray.CreateBuilder<QsFunc>();
        }

        public void AddType(QsMetaItemId typeId,
            ImmutableArray<string> typeParams,
            QsTypeValue? baseTypeValue,
            ImmutableArray<QsMetaItemId> memberTypeIds,
            ImmutableArray<QsMetaItemId> staticMemberFuncIds,
            ImmutableArray<QsMetaItemId> staticMemberVarIds,
            ImmutableArray<QsMetaItemId> memberFuncIds,
            ImmutableArray<QsMetaItemId> memberVarIds)
        {
            typesBuilder.Add(new QsDefaultType(typeId, typeParams, baseTypeValue, memberTypeIds, staticMemberFuncIds, staticMemberVarIds, memberFuncIds, memberVarIds));
        }

        public void AddFunc(QsMetaItemId funcId, bool bSeqCall, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            funcsBuilder.Add(new QsFunc(funcId, bSeqCall, bThisCall, typeParams, retTypeValue, paramTypeValues));
        }

        public QsMetadata ToMetadata()
        {
            return new QsMetadata(moduleName, typesBuilder.ToImmutable(), funcsBuilder.ToImmutable(), ImmutableArray<QsVariable>.Empty);
        }
    }

    public class QsNativeModuleBuilder
    {
        public ImmutableArray<IQsModuleTypeInfo>.Builder ModuleTypeInfosBuilder { get; }
        public ImmutableArray<IQsModuleFuncInfo>.Builder ModuleFuncInfosBuilder { get; }

        public QsNativeModuleBuilder()
        {
            ModuleTypeInfosBuilder = ImmutableArray.CreateBuilder<IQsModuleTypeInfo>();
            ModuleFuncInfosBuilder = ImmutableArray.CreateBuilder<IQsModuleFuncInfo>();
        }

        public void AddTypeInstantiator(QsMetaItemId typeId, QsNativeTypeInstantiator instantiator)
        {
            ModuleTypeInfosBuilder.Add(new QsNativeModuleTypeInfo(typeId, instantiator));
        }

        public void AddFuncInstantiator(QsMetaItemId funcId, QsNativeFuncInstantiator instantiator)
        {
            ModuleFuncInfosBuilder.Add(new QsNativeModuleFuncInfo(funcId, instantiator));
        }
    }

    public class QsRuntimeModuleBuilder : QsNativeModuleBuilder
    {
        string moduleName;

        public QsRuntimeModuleBuilder(string moduleName)
        {
            this.moduleName = moduleName;
        }

        public QsRuntimeModule ToRuntimeModule()
        {
            return new QsRuntimeModule(moduleName, ModuleTypeInfosBuilder.ToImmutable(), ModuleFuncInfosBuilder.ToImmutable());
        }
    }

    public class QsNativeObjectInfo : IQsNativeObjectInfo
    {
        List<QsNativeType> nativeTypes;
        List<QsNativeFunc> nativeFuncs;

        public QsNativeObjectInfo()
        {
            nativeTypes = new List<QsNativeType>();
            nativeFuncs = new List<QsNativeFunc>();
        }

        protected void AddNativeFunc(QsNativeFunc nativeFunc)
        {
            nativeFuncs.Add(nativeFunc);
        }

        protected void AddNativeType(QsNativeType nativeType)
        {
            nativeTypes.Add(nativeType);
        }

        public void BuildMeta(QsNativeMetaBuilder builder)
        {
            foreach (var nativeType in nativeTypes)
                builder.AddType(
                    nativeType.TypeId,
                    nativeType.TypeParams,
                    nativeType.BaseTypeValue,
                    nativeType.MemberTypeIds,
                    nativeType.StaticMemberFuncIds,
                    nativeType.StaticMemberVarIds,
                    nativeType.MemberFuncIds,
                    nativeType.MemberVarIds);

            foreach (var nativeFunc in nativeFuncs)
                builder.AddFunc(nativeFunc.FuncId, nativeFunc.bSeqCall, nativeFunc.bThisCall, nativeFunc.TypeParams, nativeFunc.RetTypeValue, nativeFunc.ParamTypeValues);
        }

        public void BuildModule(QsNativeModuleBuilder builder)
        {
            foreach (var nativeType in nativeTypes)
                builder.AddTypeInstantiator(nativeType.TypeId, nativeType.Instantiator);

            foreach (var nativeFunc in nativeFuncs)
                builder.AddFuncInstantiator(nativeFunc.FuncId, nativeFunc.Instantiator);
        }
    }

    public class QsListObjectInfo : QsNativeObjectInfo
    {   
        public QsListObjectInfo()
        {
            QsTypeValue intTypeValue = new QsNormalTypeValue(null, QsRuntimeModuleInfo.IntId);
            QsTypeValue listElemTypeValue = new QsTypeVarTypeValue(QsRuntimeModuleInfo.ListId, "T");

            var memberFuncIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            // List<T>.Add
            AddFunc(QsRuntimeModuleInfo.ListId.Append("Add", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, ImmutableArray.Create(listElemTypeValue), QsListObject.NativeAdd);

            // List<T>.RemoveAt(int index)     
            AddFunc(QsRuntimeModuleInfo.ListId.Append("RemoveAt", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, ImmutableArray.Create(intTypeValue), QsListObject.NativeRemoveAt);

            // Enumerator<T> List<T>.GetEnumerator()
            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args) => QsListObject.NativeGetEnumerator(domainService, QsRuntimeModuleInfo.EnumeratorId, typeArgs, thisValue, args);

            AddFunc(QsRuntimeModuleInfo.ListId.Append("GetEnumerator", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                new QsNormalTypeValue(null, QsRuntimeModuleInfo.EnumeratorId, listElemTypeValue), ImmutableArray<QsTypeValue>.Empty, wrappedGetEnumerator);

            // T List<T>.Indexer(int index)
            AddFunc(QsRuntimeModuleInfo.ListId.Append(QsSpecialName.Indexer, 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                listElemTypeValue, ImmutableArray.Create(intTypeValue), QsListObject.NativeIndexer);

            AddNativeType(new QsNativeType(
                QsRuntimeModuleInfo.ListId,
                ImmutableArray.Create("T"), // typeParams
                null,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                memberFuncIdsBuilder.ToImmutable(),
                ImmutableArray<QsMetaItemId>.Empty,
                new QsNativeTypeInstantiator(() => new QsObjectValue(null))));

            return;

            void AddFunc(
                QsMetaItemId funcId,
                bool bSeqCall, bool bThisCall,
                ImmutableArray<string> typeParams,
                QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues,
                Invoker invoker)
            {
                var nativeFunc = new QsNativeFunc(funcId, bSeqCall, bThisCall, typeParams, retTypeValue, paramTypeValues, new QsNativeFuncInstantiator(bThisCall, invoker));

                AddNativeFunc(nativeFunc);
                memberFuncIdsBuilder.Add(nativeFunc.FuncId);
            }
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
        internal static ValueTask<QsValue> NativeGetEnumerator(QsDomainService domainService, QsMetaItemId enumeratorId, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
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

        internal static ValueTask<QsValue> NativeIndexer(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);

            return new ValueTask<QsValue>(list.Elems[((QsValue<int>)args[0]).Value]);
        }

        // List<T>.Add
        internal static ValueTask<QsValue> NativeAdd(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
        {
            Debug.Assert(args.Length == 1);
            Debug.Assert(thisValue != null);

            var list = GetObject<QsListObject>(thisValue);
            list.Elems.Add(args[0]);

            return new ValueTask<QsValue>(QsVoidValue.Instance);
        }

        internal static ValueTask<QsValue> NativeRemoveAt(QsDomainService domainService, QsTypeEnv typeEnv, QsValue? thisValue, ImmutableArray<QsValue> args)
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
