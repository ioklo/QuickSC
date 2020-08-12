using System.Linq;

namespace QuickSC
{
    public class QsEnumTypeInst : QsTypeInst
    {
        QsTypeValue.Normal typeValue;
        string defaultElemName;

        public QsEnumTypeInst(QsTypeValue.Normal typeValue, string defaultElemName)
        {
            this.typeValue = typeValue;
            this.defaultElemName = defaultElemName;
        }

        public override QsTypeValue GetTypeValue()
        {
            return typeValue;
        }

        public override QsValue MakeDefaultValue()
        {
            return new QsEnumValue(defaultElemName, Enumerable.Empty<(string, QsValue)>() );
        }
    }
}