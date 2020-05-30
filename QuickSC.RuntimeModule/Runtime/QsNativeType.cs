using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsNativeType
    {
        public QsType Type { get; }
        public QsValue DefaultValue { get; }

        public QsNativeType(QsType type, QsValue defaultValue)
        {
            Type = type;
            DefaultValue = defaultValue;
        }
    }
}
