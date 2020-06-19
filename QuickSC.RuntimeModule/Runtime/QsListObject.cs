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
    
    public class QsRuntimeModuleBuilder
    {
        ImmutableArray<QsType>.Builder typesBuilder;
        ImmutableArray<QsFunc>.Builder funcsBuilder;
        ImmutableArray<QsVariable>.Builder varsBuilder;

        public ImmutableArray<IQsModuleTypeInfo>.Builder ModuleTypeInfosBuilder { get; }
        public ImmutableArray<IQsModuleFuncInfo>.Builder ModuleFuncInfosBuilder { get; }

        public QsRuntimeModuleBuilder()
        {
            typesBuilder = ImmutableArray.CreateBuilder<QsType>();
            funcsBuilder = ImmutableArray.CreateBuilder<QsFunc>();
            varsBuilder = ImmutableArray.CreateBuilder<QsVariable>();

            ModuleTypeInfosBuilder = ImmutableArray.CreateBuilder<IQsModuleTypeInfo>();
            ModuleFuncInfosBuilder = ImmutableArray.CreateBuilder<IQsModuleFuncInfo>();            
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

        public void AddType(QsDotnetType dotnetType)
        {
            typesBuilder.Add(dotnetType);
        }

        public void AddVar(QsMetaItemId varId, QsTypeValue typeValue)
        {
            varsBuilder.Add(new QsVariable(false, varId, typeValue));
        }

        public void AddFunc(QsMetaItemId funcId, bool bSeqCall, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retTypeValue, ImmutableArray<QsTypeValue> paramTypeValues)
        {
            funcsBuilder.Add(new QsFunc(funcId, bSeqCall, bThisCall, typeParams, retTypeValue, paramTypeValues));
        }

        public void AddTypeInstantiator(QsMetaItemId typeId, QsNativeTypeInstantiator instantiator)
        {
            ModuleTypeInfosBuilder.Add(new QsNativeModuleTypeInfo(typeId, instantiator));
        }

        public void AddFuncInstantiator(QsMetaItemId funcId, QsNativeFuncInstantiator instantiator)
        {
            ModuleFuncInfosBuilder.Add(new QsNativeModuleFuncInfo(funcId, instantiator));
        }

        public IEnumerable<QsType> GetTypes() { return typesBuilder; }
        public IEnumerable<QsFunc> GetFuncs() { return funcsBuilder; }
        public IEnumerable<QsVariable> GetVars() { return varsBuilder; }        
    }

    public class QsRuntimeModuleObjectInfo : IQsRuntimeModuleObjectInfo
    {
        List<QsNativeType> nativeTypes;
        List<QsNativeFunc> nativeFuncs;
        List<QsNativeVar> nativeVars;

        public QsRuntimeModuleObjectInfo()
        {
            nativeTypes = new List<QsNativeType>();
            nativeFuncs = new List<QsNativeFunc>();
            nativeVars = new List<QsNativeVar>();
        }

        protected void AddNativeVar(QsNativeVar nativeVar)
        {
            nativeVars.Add(nativeVar);
        }

        protected void AddNativeFunc(QsNativeFunc nativeFunc)
        {
            nativeFuncs.Add(nativeFunc);
        }

        protected void AddNativeType(QsNativeType nativeType)
        {
            nativeTypes.Add(nativeType);
        }
        
        public void BuildModule(QsRuntimeModuleBuilder builder)
        {
            foreach (var nativeType in nativeTypes)
            {
                builder.AddType(
                    nativeType.TypeId,
                    nativeType.TypeParams,
                    nativeType.BaseTypeValue,
                    nativeType.MemberTypeIds,
                    nativeType.StaticMemberFuncIds,
                    nativeType.StaticMemberVarIds,
                    nativeType.MemberFuncIds,
                    nativeType.MemberVarIds);

                builder.AddTypeInstantiator(nativeType.TypeId, nativeType.Instantiator);
            }

            foreach (var nativeFunc in nativeFuncs)
            {
                builder.AddFunc(nativeFunc.FuncId, nativeFunc.bSeqCall, nativeFunc.bThisCall, nativeFunc.TypeParams, nativeFunc.RetTypeValue, nativeFunc.ParamTypeValues);
                builder.AddFuncInstantiator(nativeFunc.FuncId, nativeFunc.Instantiator);
            }

            foreach(var nativeVar in nativeVars)
            {
                builder.AddVar(nativeVar.VarId, nativeVar.TypeValue);
            }
        }
    }

    public class QsListObjectInfo : QsRuntimeModuleObjectInfo
    {   
        public QsListObjectInfo()
        {
            QsTypeValue intTypeValue = new QsNormalTypeValue(null, QsRuntimeModule.IntId);
            QsTypeValue listElemTypeValue = new QsTypeVarTypeValue(QsRuntimeModule.ListId, "T");

            var memberFuncIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            // List<T>.Add
            AddFunc(QsRuntimeModule.ListId.Append("Add", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, ImmutableArray.Create(listElemTypeValue), QsListObject.NativeAdd);

            // List<T>.RemoveAt(int index)     
            AddFunc(QsRuntimeModule.ListId.Append("RemoveAt", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                QsVoidTypeValue.Instance, ImmutableArray.Create(intTypeValue), QsListObject.NativeRemoveAt);

            // Enumerator<T> List<T>.GetEnumerator()
            Invoker wrappedGetEnumerator =
                (domainService, typeArgs, thisValue, args) => QsListObject.NativeGetEnumerator(domainService, QsRuntimeModule.EnumeratorId, typeArgs, thisValue, args);

            AddFunc(QsRuntimeModule.ListId.Append("GetEnumerator", 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                new QsNormalTypeValue(null, QsRuntimeModule.EnumeratorId, listElemTypeValue), ImmutableArray<QsTypeValue>.Empty, wrappedGetEnumerator);

            // T List<T>.Indexer(int index)
            AddFunc(QsRuntimeModule.ListId.Append(QsSpecialName.Indexer, 0),
                bSeqCall: false, bThisCall: true, ImmutableArray<string>.Empty,
                listElemTypeValue, ImmutableArray.Create(intTypeValue), QsListObject.NativeIndexer);

            AddNativeType(new QsNativeType(
                QsRuntimeModule.ListId,
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
