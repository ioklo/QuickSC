namespace QuickSC.Runtime
{
    public interface IQsGlobalVarRepo
    {
        QsValue GetValue(QsMetaItemId varId);
        void SetValue(QsMetaItemId varId, QsValue value);
    }
}