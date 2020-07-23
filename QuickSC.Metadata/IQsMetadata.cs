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
        
        bool GetTypeInfo(QsMetaItemId id, [NotNullWhen(returnValue: true)] out QsTypeInfo? typeInfo);
        bool GetFuncInfo(QsMetaItemId id, [NotNullWhen(returnValue: true)] out QsFuncInfo? funcInfo);
        bool GetVarInfo(QsMetaItemId id, [NotNullWhen(returnValue: true)] out QsVarInfo? varInfo);
    }
}
