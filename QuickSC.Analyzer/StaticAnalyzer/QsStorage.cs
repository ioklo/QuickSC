namespace QuickSC.StaticAnalyzer
{
    public enum QsStorageKind
    {
        Func,
        Var,
    }

    public abstract class QsStorage
    {    
    }

    public class QsGlobalStorage : QsStorage
    {
        public IQsMetadata? Metadata { get; }        
        public QsGlobalStorage(IQsMetadata? metadata)            
        {
            Metadata = metadata;
        }
    }

    public class QsLocalStorage : QsStorage
    {
        static public QsLocalStorage Instance { get; } = new QsLocalStorage();
        private QsLocalStorage() { }
    }

    public class QsInstanceStorage : QsStorage
    {
        static public QsInstanceStorage Instance { get; } = new QsInstanceStorage();
        private QsInstanceStorage() { }
    }

    public class QsStaticStorage : QsStorage
    {
        public QsTypeValue TypeValue { get; }
        public QsStaticStorage(QsTypeValue typeValue)
        {
            TypeValue = typeValue;
        }
    }
}