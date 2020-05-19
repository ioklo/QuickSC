using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public class QsVariable
    {
        public QsVarId VarId { get; }
        public QsTypeValue TypeValue { get; }
        public string Name { get; }

        public QsVariable(QsVarId varId, QsTypeValue typeValue, string name)
        {
            VarId = varId;
            TypeValue = typeValue;
            Name = name;
        }
    }
}
