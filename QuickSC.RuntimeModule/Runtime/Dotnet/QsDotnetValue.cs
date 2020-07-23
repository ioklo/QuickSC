using System.Reflection;

namespace QuickSC.Runtime.Dotnet
{
    class QsDotnetValue : QsValue
    {
        object? obj;

        public QsDotnetValue(object? obj)
        {
            this.obj = obj;
        }

        public QsValue GetMemberValue(QsName varName)
        {
            var fieldInfo = obj!.GetType().GetField(varName.Name);

            return new QsDotnetValue(fieldInfo.GetValue(obj));
        }

        public QsTypeInst GetTypeInst()
        {
            return new QsDotnetTypeInst(obj!.GetType().GetTypeInfo());
        }

        public override QsValue MakeCopy()
        {
            return new QsDotnetValue(obj);
        }

        public override void SetValue(QsValue fromValue)
        {
            if (fromValue is QsDotnetValue dotnetFromValue)
            {
                this.obj = dotnetFromValue.obj;
            }
        }
    }
}
