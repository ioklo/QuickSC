namespace QuickSC.StaticAnalyzer
{
    public struct QsAnalyzerVarInfo
    {
        public QsStorage Storage { get; }
        public QsTypeValue TypeValue { get; }
        public QsAnalyzerVarInfo(QsStorage storage, QsTypeValue typeValue)
        {
            Storage = storage;
            TypeValue = typeValue;
        }
    }
}