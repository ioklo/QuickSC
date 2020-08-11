using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsAnalyzer
    {
        public class LocalVarInfo
        {
            public int Index { get; }
            public QsTypeValue TypeValue { get; }

            public LocalVarInfo(int index, QsTypeValue typeValue)
            {
                Index = index;
                TypeValue = typeValue;
            }
        }
    }
}
