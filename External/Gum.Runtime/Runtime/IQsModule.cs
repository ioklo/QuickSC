using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModule : IModuleInfo
    {
        void OnLoad(QsDomainService domainService);

        QsTypeInst GetTypeInst(QsDomainService domainService, TypeValue.Normal typeValue);
        QsFuncInst GetFuncInst(QsDomainService domainService, FuncValue funcValue);
    }
}
