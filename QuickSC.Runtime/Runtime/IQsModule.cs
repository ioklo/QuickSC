using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModule : IQsMetadata
    {
        QsFuncInst GetFuncInst(QsFuncValue funcValue);
    }
}
