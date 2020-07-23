using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzerLocalVarInfo
    {
        public int Index { get; }
        public QsTypeValue TypeValue { get; }

        public QsAnalyzerLocalVarInfo(int index, QsTypeValue typeValue)
        {
            Index = index;
            TypeValue = typeValue;
        }
    }
}
