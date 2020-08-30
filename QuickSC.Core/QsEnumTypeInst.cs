using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC
{
    public class QsEnumTypeInst : QsTypeInst
    {
        QsTypeValue.Normal typeValue;
        string defaultElemName;
        (string Name, QsTypeInst TypeInst)[] defaultFieldInsts;

        public QsEnumTypeInst(QsTypeValue.Normal typeValue, string defaultElemName, IEnumerable<(string Name, QsTypeInst TypeInst)> defaultFieldInsts)
        {
            this.typeValue = typeValue;
            this.defaultElemName = defaultElemName;
            this.defaultFieldInsts = defaultFieldInsts.ToArray();
        }

        public override QsTypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override QsValue MakeDefaultValue()
        {
            return new QsEnumValue(defaultElemName, defaultFieldInsts.Select(e => (e.Name, e.TypeInst.MakeDefaultValue())));
        }
    }
}