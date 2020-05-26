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

        public QsNameElem(QsSpecialName specialName, int typeParamCount)
        {
            Name = new QsName(specialName);
            TypeParamCount = typeParamCount;
        }

        public QsNameElem(string name, int typeParamCount)
        {
            Name = new QsName(name);
            TypeParamCount = typeParamCount;
        }
    }
}