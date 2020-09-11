using Gum.CompileTime;
using System;

namespace QuickSC.Runtime.Dotnet
{
    // 
    class QsDotnetObject : QsObject
    {
        QsTypeInst typeInst;        
        Object obj;

        public QsDotnetObject(QsTypeInst typeInst, Object obj)
        {
            this.typeInst = typeInst;
            this.obj = obj;
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }

        public override QsValue GetMemberValue(Name varName)
        {
            var fieldInfo = obj.GetType().GetField(varName.Text!);
            return new QsDotnetValue(fieldInfo.GetValue(obj));
        }
    }
}
