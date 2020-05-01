using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    public interface IQsCommandProvider
    {
        Task ExecuteAsync(string cmdText);
    }
}
