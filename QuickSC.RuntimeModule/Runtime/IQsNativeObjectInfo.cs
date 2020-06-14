namespace QuickSC.Runtime
{
    public interface IQsNativeObjectInfo
    {
        void BuildMeta(QsNativeMetaBuilder builder);
        void BuildModule(QsNativeModuleBuilder builder);
    }
}