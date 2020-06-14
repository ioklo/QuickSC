using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModuleInfo
    {
        IQsMetadata GetMetadata();
        IQsModule MakeModule(QsDomainService domainService/*, IQsGlobalVarRepo globalVarRepo*/);
    }
}
