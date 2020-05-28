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
    }
}