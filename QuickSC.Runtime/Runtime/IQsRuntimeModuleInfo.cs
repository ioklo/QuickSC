using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsRuntimeModuleInfo : IQsModuleInfo
    {
        IQsRuntimeModule MakeRuntimeModule(/*IQsGlobalVarRepo globalVarRepo*/);
    }
}
