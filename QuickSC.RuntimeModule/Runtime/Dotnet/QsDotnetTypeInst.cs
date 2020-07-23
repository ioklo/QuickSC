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

        public override QsTypeValue GetTypeValue()
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
