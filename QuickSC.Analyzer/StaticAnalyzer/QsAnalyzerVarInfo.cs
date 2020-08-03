namespace QuickSC.StaticAnalyzer
{
    public struct QsAnalyzerVarInfo
    {
        public QsStorageInfo StorageInfo { get; }
        public QsTypeValue TypeValue { get; }
        public QsAnalyzerVarInfo(QsStorageInfo storageInfo, QsTypeValue typeValue)
        {
            StorageInfo = storageInfo;
            TypeValue = typeValue;
        }
    }
}