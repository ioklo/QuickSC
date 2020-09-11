using Gum.CompileTime;
using System.Reflection;

namespace Gum.Runtime.Dotnet
{
    class QsDotnetValue : Value
    {
        object? obj;

        public QsDotnetValue(object? obj)
        {
            this.obj = obj;
        }

        public Value GetMemberValue(Name varName)
        {
            var fieldInfo = obj!.GetType().GetField(varName.Text);

            return new QsDotnetValue(fieldInfo.GetValue(obj));
        }

        public TypeInst GetTypeInst()
        {
            return new QsDotnetTypeInst(obj!.GetType().GetTypeInfo());
        }

        public override Value MakeCopy()
        {
            return new QsDotnetValue(obj);
        }

        public override void SetValue(Value fromValue)
        {
            if (fromValue is QsDotnetValue dotnetFromValue)
            {
                this.obj = dotnetFromValue.obj;
            }
        }
    }
}
