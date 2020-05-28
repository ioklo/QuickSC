using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsVariable
    {
        public bool bStatic { get; }
        public QsVarId VarId { get; }
        public QsTypeValue TypeValue { get; }

        public QsVariable(bool bStatic, QsVarId varId, QsTypeValue typeValue)
        {
            this.bStatic = bStatic;
            VarId = varId;
            TypeValue = typeValue;
        }
    }
}
