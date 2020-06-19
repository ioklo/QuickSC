using QuickSC.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace QuickSC.ModuleTest
{
    class QsTestModule : IQsModule
    {
        public string ModuleName { get => "TestModule"; }

        public IEnumerable<QsType> Types { get; }
        public IEnumerable<QsFunc> Funcs { get; }
        public IEnumerable<QsVariable> Vars { get; }

        public IEnumerable<IQsModuleTypeInfo> TypeInfos { get; }
        public IEnumerable<IQsModuleFuncInfo> FuncInfos { get; }

        public void OnLoad(QsDomainService domainService)
        {

        }
    }

    public class QsModuleTest
    {
        [Fact]
        void TestGetGlobalVariable()
        {
            var testModule = new QsTestModule();

            // external과 internal을 섞어서 더 복잡하게 보인다
            var metadataService = new QsMetadataService(
                Enumerable.Empty<QsType>(), 
                Enumerable.Empty<QsFunc>(), 
                Enumerable.Empty<QsVariable>(), 
                ImmutableArray.Create<IQsMetadata>(testModule));

            var domainService = new QsDomainService(metadataService);

            domainService.LoadModule(testModule);

            // globalVariable x
            // var x = domainService.GetValue(new QsMetaItemId(new QsMetaItemIdElem("x", 0)));
        }
    }
}

