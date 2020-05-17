using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC
{
    public interface IQsMetadata
    {
        bool GetGlobalFuncTypeValue(string value, ImmutableArray<QsTypeValue> typeArgs, [NotNullWhen(returnValue: true)] out QsFuncTypeValue? outFuncTypeValue);
        bool GetGlobalVarTypeValue(string value, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue);

        bool GetGlobalType(string name, int typeParamCount, [NotNullWhen(returnValue: true)] out QsType? type);
    }
}
