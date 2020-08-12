using QuickSC.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace QuickSC.ModuleTest
{
    class QsTestModule : IQsModule
    {
        public string ModuleName { get => "TestModule"; }

        public bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(true)] out QsFuncInfo? funcInfo)
        {
            throw new NotImplementedException();
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
        {
            throw new NotImplementedException();
        }

        public bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(true)] out IQsTypeInfo? typeInfo)
        {
            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue.Normal typeValue)
        {
            throw new NotImplementedException();
        }

        public bool GetVarInfo(QsMetaItemId id, [NotNullWhen(true)] out QsVarInfo? varInfo)
        {
            throw new NotImplementedException();
        }

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
            var domainService = new QsDomainService();

            domainService.LoadModule(testModule);

            // globalVariable x
            // var x = domainService.GetValue(QsMetaItemId.Make(new QsMetaItemIdElem("x", 0)));
        }
    }
}

