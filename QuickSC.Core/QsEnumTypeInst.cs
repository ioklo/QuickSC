using Gum.CompileTime;
using Gum.Runtime;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC
{
    public class QsEnumTypeInst : TypeInst
    {
        TypeValue.Normal typeValue;
        string defaultElemName;
        (string Name, TypeInst TypeInst)[] defaultFieldInsts;

        public QsEnumTypeInst(TypeValue.Normal typeValue, string defaultElemName, IEnumerable<(string Name, TypeInst TypeInst)> defaultFieldInsts)
        {
            this.typeValue = typeValue;
            this.defaultElemName = defaultElemName;
            this.defaultFieldInsts = defaultFieldInsts.ToArray();
        }

        public override TypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override Value MakeDefaultValue()
        {
            return new EnumValue(defaultElemName, defaultFieldInsts.Select(e => (e.Name, e.TypeInst.MakeDefaultValue())));
        }
    }
}