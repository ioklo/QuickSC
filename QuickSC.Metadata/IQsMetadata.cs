using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC
{
    public interface IQsMetadata
    {
        bool GetGlobalType(string name, int typeParamCount, [NotNullWhen(returnValue: true)] out QsType? outType);
        bool GetGlobalFunc(string name, [NotNullWhen(returnValue: true)] out QsFunc? outFunc);
        bool GetGlobalVar(string name, [NotNullWhen(returnValue: true)] out QsVariable? outVar);

        bool GetTypeById(QsTypeId typeId, [NotNullWhen(returnValue: true)] out QsType? outType);
        bool GetFuncById(QsFuncId funcId, [NotNullWhen(returnValue: true)] out QsFunc? outFunc);
        bool GetVarById(QsVarId typeId, [NotNullWhen(returnValue: true)] out QsVariable? outVar);
    }
}
