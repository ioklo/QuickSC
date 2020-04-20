using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public interface IQsCommandProvider
    {
        void Execute(string nameStr, ImmutableArray<string> argStrs);
    }
}
