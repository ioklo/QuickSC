using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC
{
    public interface IQsMetadata
    {
        bool GetGlobalTypeValue(string name, ImmutableArray<QsTypeValue> typeArgs, [NotNullWhen(returnValue:true)] out QsTypeValue? globalTypeValue);
    }
}
