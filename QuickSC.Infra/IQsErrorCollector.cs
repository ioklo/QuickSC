using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public interface IQsErrorCollector
    {
        void Add(object obj, string msg);
        bool HasError { get; }
    }
}
