using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsRuntimeModuleInfo : IQsRuntimeModuleInfo
    {
        public const string MODULE_NAME = "System.Runtime";
        public string ModuleName => MODULE_NAME;

        public static QsMetaItemId BoolId = new QsMetaItemId(new QsMetaItemIdElem("bool", 0));
        public static QsMetaItemId IntId = new QsMetaItemId(new QsMetaItemIdElem("int", 0));
        public static QsMetaItemId StringId = new QsMetaItemId(new QsMetaItemIdElem("string", 0));
        public static QsMetaItemId ListId = new QsMetaItemId(new QsMetaItemIdElem("List", 1));
        public static QsMetaItemId EnumerableId = new QsMetaItemId(new QsMetaItemIdElem("Enumerable", 1));
        public static QsMetaItemId EnumeratorId = new QsMetaItemId(new QsMetaItemIdElem("Enumerator", 1));

        // TODO: localId와 globalId를 나눠야 할 것 같다. 내부에서는 LocalId를 쓰고, Runtime은 GlobalId로 구분해야 할 것 같다
        public QsVariable EnvVar { get; }

        List<IQsNativeObjectInfo> objInfos;

        class QsEmptyObjectInfo : IQsNativeObjectInfo
        {
            private QsMetaItemId typeId;
            private Func<QsValue> defaultValueFactory;

            public QsEmptyObjectInfo(QsMetaItemId typeId, Func<QsValue> defaultValueFactory)
            {
                this.typeId = typeId;
                this.defaultValueFactory = defaultValueFactory;
            }

            public void BuildMeta(QsNativeMetaBuilder builder)
            {
                builder.AddType(
                    typeId,
                    ImmutableArray<string>.Empty,
                    null,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty,
                    ImmutableArray<QsMetaItemId>.Empty);
            }

            public void BuildModule(QsNativeModuleBuilder builder)
            {
                builder.AddTypeInstantiator(typeId, new QsNativeTypeInstantiator(defaultValueFactory));
            }
        }

        public QsRuntimeModuleInfo()
        {   
            var envTypeId = new QsMetaItemId(new QsMetaItemIdElem("Environment", 0));

            objInfos = new List<IQsNativeObjectInfo>();
            objInfos.Add(new QsEmptyObjectInfo(BoolId, () => new QsValue<bool>(false)));
            objInfos.Add(new QsEmptyObjectInfo(IntId, () => new QsValue<int>(0)));
            objInfos.Add(new QsEmptyObjectInfo(StringId, () => new QsObjectValue(null)));
            objInfos.Add(new QsEnumerableObjectInfo());
            objInfos.Add(new QsEnumeratorObjectInfo());
            objInfos.Add(new QsListObjectInfo());
            objInfos.Add(new QsDotnetObjectInfo(envTypeId, typeof(QsEnvironment)));
            //             
            // typeBuilder.AddType(new QsDotnetType(envTypeId, typeof(QsEnvironment)), new QsObjectValue(null));            

            EnvVar = new QsVariable(false, new QsMetaItemId(new QsMetaItemIdElem("env", 0)), new QsNormalTypeValue(null, envTypeId));

            // objectInfo를 돌면서
        }

        public IQsMetadata GetMetadata()
        {
            var builder = new QsNativeMetaBuilder(ModuleName);
            foreach (var objInfo in objInfos)
            {
                objInfo.BuildMeta(builder);
            }

            return builder.ToMetadata();
        }

        public IQsModule MakeModule(/*IQsGlobalVarRepo globalVarRepo*/)
            => MakeRuntimeModule();


        public IQsRuntimeModule MakeRuntimeModule(/*IQsGlobalVarRepo globalVarRepo*/)
        {
            var builder = new QsRuntimeModuleBuilder(ModuleName);

            foreach (var objInfo in objInfos)
                objInfo.BuildModule(builder);

            return builder.ToRuntimeModule();
        }
    }
}
