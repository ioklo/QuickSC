using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC
{
    public interface IQsMetadata
    {
        string ModuleName { get; }

        // TODO: *Id에는 ModuleName이 있는데, 빼고 인자로 받을 수 있게 해야 할 것 같다
        bool GetTypeById(QsTypeId typeId, [NotNullWhen(returnValue: true)] out QsType? outType);
        bool GetFuncById(QsFuncId funcId, [NotNullWhen(returnValue: true)] out QsFunc? outFunc);
        bool GetVarById(QsVarId typeId, [NotNullWhen(returnValue: true)] out QsVariable? outVar);
    }
}
