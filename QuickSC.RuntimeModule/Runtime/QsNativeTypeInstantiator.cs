using Gum.CompileTime;
using System;

namespace QuickSC.Runtime
{
    public class QsNativeTypeInstantiator
    {
        public ModuleItemId TypeId { get; }
        Func<QsValue> defaultValueFactory;

        public QsNativeTypeInstantiator(ModuleItemId typeId, Func<QsValue> defaultValueFactory)
        {
            TypeId = typeId;
            this.defaultValueFactory = defaultValueFactory;
        }

        public QsTypeInst Instantiate(QsDomainService domainService, TypeValue.Normal ntv)
        {
            return new QsNativeTypeInst(ntv, defaultValueFactory);
        }
    }
}