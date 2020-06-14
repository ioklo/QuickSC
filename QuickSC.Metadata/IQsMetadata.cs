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

        IEnumerable<QsType> Types { get; }
        IEnumerable<QsFunc> Funcs { get; }
        IEnumerable<QsVariable> Vars { get; }
    }
}
