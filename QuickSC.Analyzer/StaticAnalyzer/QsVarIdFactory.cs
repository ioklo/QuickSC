using System;

namespace QuickSC.StaticAnalyzer
{
    public class QsVarIdFactory
    {
        int varIdCount;

        public QsVarIdFactory()
        {
            varIdCount = 0;
        }

        public QsVarId MakeVarId()
        {
            return new QsVarId(null, varIdCount);
        }
    }
}