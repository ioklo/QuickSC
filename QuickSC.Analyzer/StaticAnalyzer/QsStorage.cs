namespace QuickSC.StaticAnalyzer
{   
    // Global, Static, Local, Instance
    // GlobalFunc, GlobalVar, 
    // LoadVariable.. 정도가 맞는것 같기도 하다
    public abstract class QsStorage
    {    
    }

    public class QsGlobalVarStorage : QsStorage
    {
        public QsVarId VarId { get; }

        public QsGlobalVarStorage(QsVarId varId)
        {
            VarId = varId;
        }
    }

    public class QsLocalVarStorage : QsStorage
    {
        public int LocalIndex { get; }
        public QsLocalVarStorage(int localIndex) { LocalIndex = localIndex; }
    }

    public class QsInstanceVarStorage : QsStorage
    {
        public QsVarId VarId { get; }
        public QsInstanceVarStorage(QsVarId varId) 
        {
            VarId = varId;
        }
    }

    public class QsInstanceFuncStorage : QsStorage
    {
        public QsFuncId FuncId { get; }
        public QsInstanceFuncStorage(QsFuncId funcId)
        {
            FuncId = funcId;
        }
    }

    public class QsStaticVarStorage : QsStorage
    {
        public QsTypeValue TypeValue { get; }
        public QsVarId VarId { get; }
        public QsStaticVarStorage(QsTypeValue typeValue, QsVarId varId)
        {
            TypeValue = typeValue;
            VarId = varId;
        }
    }

    public class QsStaticFuncStorage : QsStorage
    {
        public QsTypeValue TypeValue { get; }
        public QsFuncId FuncId { get; }
        public QsStaticFuncStorage(QsTypeValue typeValue, QsFuncId funcId)
        {
            TypeValue = typeValue;
            FuncId = funcId;
        }
    }
}