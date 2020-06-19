using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public struct QsMetaItemIdElem
    {
        public QsName Name { get; }
        public int TypeParamCount { get; }
        
        public QsMetaItemIdElem(QsName name, int typeParamCount = 0)
        {
            Name = name;
            TypeParamCount = typeParamCount;
        }

        public QsMetaItemIdElem(string name, int typeParamCount = 0)
        {
            Name = QsName.Text(name);
            TypeParamCount = typeParamCount;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Name);

            if (TypeParamCount != 0)
            {
                sb.Append('<');
                for (int i = 0; i < TypeParamCount - 1; i++)
                    sb.Append(',');
                sb.Append('>');
            }

            return sb.ToString();
        }
    }
}