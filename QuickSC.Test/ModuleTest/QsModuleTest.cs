using Gum;
using Gum.CompileTime;
using Gum.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace QuickSC.ModuleTest
{
    class QsTestModule : IModule
    {
        public string ModuleName { get => "TestModule"; }

        public bool GetFuncInfo(ModuleItemId id, [NotNullWhen(true)] out FuncInfo? funcInfo)
        {
            throw new NotImplementedException();
        }

        public FuncInst GetFuncInst(DomainService domainService, FuncValue funcValue)
        {
            throw new NotImplementedException();
        }

        public bool GetTypeInfo(ModuleItemId id, [NotNullWhen(true)] out ITypeInfo? typeInfo)
        {
            throw new NotImplementedException();
        }

        public TypeInst GetTypeInst(DomainService domainService, TypeValue.Normal typeValue)
        {
            throw new NotImplementedException();
        }

        public bool GetVarInfo(ModuleItemId id, [NotNullWhen(true)] out VarInfo? varInfo)
        {
            throw new NotImplementedException();
        }

        public void OnLoad(DomainService domainService)
        {

        }
    }

    public class QsModuleTest
    {
        [Fact]
        void TestGetGlobalVariable()
        {
            var testModule = new QsTestModule();            
            var domainService = new DomainService();

            domainService.LoadModule(testModule);

            // globalVariable x
            // var x = domainService.GetValue(QsMetaItemId.Make(new QsMetaItemIdElem("x", 0)));
        }
    }
}

