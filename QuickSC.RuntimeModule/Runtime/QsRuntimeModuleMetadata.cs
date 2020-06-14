using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace QuickSC.Runtime
{
    public class QsMetadata : IQsMetadata
    {
        public string ModuleName { get; }

        public IEnumerable<QsType> Types { get; }
        public IEnumerable<QsFunc> Funcs { get; }
        public IEnumerable<QsVariable> Vars { get; }

        public QsMetadata(string moduleName, ImmutableArray<QsType> types, ImmutableArray<QsFunc> funcs, ImmutableArray<QsVariable> vars)
        {
            ModuleName = moduleName;
            Types = types;
            Funcs = funcs;
            Vars = vars;
        }
    }
}
