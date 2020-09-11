using Gum.CompileTime;
using System;

namespace Gum.Runtime
{
    public class QsNativeTypeInstantiator
    {
        public ModuleItemId TypeId { get; }
        Func<Value> defaultValueFactory;

        public QsNativeTypeInstantiator(ModuleItemId typeId, Func<Value> defaultValueFactory)
        {
            TypeId = typeId;
            this.defaultValueFactory = defaultValueFactory;
        }

        public TypeInst Instantiate(DomainService domainService, TypeValue.Normal ntv)
        {
            return new QsNativeTypeInst(ntv, defaultValueFactory);
        }
    }
}