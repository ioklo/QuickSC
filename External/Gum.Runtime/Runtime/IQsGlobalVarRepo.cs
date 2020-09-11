using Gum.CompileTime;

namespace QuickSC.Runtime
{
    public interface IQsGlobalVarRepo
    {
        QsValue GetValue(ModuleItemId varId);
        void SetValue(ModuleItemId varId, QsValue value);
    }
}