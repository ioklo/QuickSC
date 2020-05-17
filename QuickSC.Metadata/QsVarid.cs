using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public struct QsVarId
    {
        public IQsMetadata? Metadata { get; }
        public int Value { get; }
        public QsVarId(IQsMetadata? metadata, int value) { Metadata = metadata; Value = value; }
    }
}
