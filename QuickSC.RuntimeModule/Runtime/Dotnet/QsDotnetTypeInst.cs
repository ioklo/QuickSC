using Gum.CompileTime;
using System;
using System.Reflection;

namespace Gum.Runtime.Dotnet
{
    class QsDotnetTypeInst : TypeInst
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

        public override Value MakeDefaultValue()
        {
            return new QsDotnetValue(null);
        }
    }
}
