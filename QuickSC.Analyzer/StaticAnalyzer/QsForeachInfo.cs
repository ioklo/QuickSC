using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsForeachInfo
    {
        public QsFuncValue GetEnumeratorValue { get; }
        public QsFuncValue MoveNextValue { get; }
        public QsFuncValue GetCurrentValue { get; }

        public QsForeachInfo(QsFuncValue getEnumeratorValue, QsFuncValue moveNextValue, QsFuncValue getCurrentValue)
        {
            GetEnumeratorValue = getEnumeratorValue;
            MoveNextValue = moveNextValue;
            GetCurrentValue = getCurrentValue;
        }
    }
}
