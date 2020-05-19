using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsForeachInfo
    {
        public QsFuncId GetEnumeratorId { get; }
        public QsFuncId MoveNextId { get; }
        public QsFuncId GetCurrentId { get; }
    }
}
