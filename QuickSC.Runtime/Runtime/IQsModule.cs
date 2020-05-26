using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModule
    {
        string ModuleName { get; }
        QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs);
        QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs);
    }
}
