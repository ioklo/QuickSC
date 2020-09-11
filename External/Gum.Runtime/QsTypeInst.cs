using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeInst
    {
        public abstract TypeValue GetTypeValue();
        public abstract QsValue MakeDefaultValue();
    }
}
