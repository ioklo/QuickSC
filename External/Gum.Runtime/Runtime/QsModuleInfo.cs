using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModuleInfo
    {
        IQsModule MakeModule(/*IQsGlobalVarRepo globalVarRepo*/);
    }
}
