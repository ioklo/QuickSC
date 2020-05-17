using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsFuncIdFactory
    {
        int funcIdCount;

        public QsFuncIdFactory()
        {
            funcIdCount = 0;
        }

        public QsFuncId MakeFuncId()
        {
            funcIdCount++;
            return new QsFuncId(null, funcIdCount);
        }
    }
}
