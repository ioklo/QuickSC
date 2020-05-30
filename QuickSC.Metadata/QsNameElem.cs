using System.Text;

namespace QuickSC
{
    public struct QsNameElem
    {
        public QsName Name { get; }
        public int TypeParamCount { get; }
        
        public QsNameElem(QsName name, int typeParamCount)
        {
            Name = name;
            TypeParamCount = typeParamCount;
        }

        public QsNameElem(string name, int typeParamCount)
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