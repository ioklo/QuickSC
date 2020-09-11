using Gum.CompileTime;
using System;
using System.Reflection;

namespace QuickSC.Runtime.Dotnet
{
    class QsDotnetTypeInst : QsTypeInst
    {
        TypeInfo typeInfo;

        public QsDotnetTypeInst(TypeInfo typeInfo)
        {
            this.typeInfo = typeInfo;
        }

        public override TypeValue GetTypeValue()
        {
            QsDotnetMisc.MakeTypeId(typeInfo.BaseType);

            throw new NotImplementedException();
        }

        public override QsValue MakeDefaultValue()
        {
            return new QsDotnetValue(null);
        }
    }
}
