using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeInst
    {
        public abstract QsTypeValue GetTypeValue();
        public abstract QsValue MakeDefaultValue();
    }
}
