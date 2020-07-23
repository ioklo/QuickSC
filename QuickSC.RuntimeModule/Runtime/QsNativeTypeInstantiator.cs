using System;

namespace QuickSC.Runtime
{
    public class QsNativeTypeInstantiator
    {
        public QsMetaItemId TypeId { get; }
        Func<QsValue> defaultValueFactory;

        public QsNativeTypeInstantiator(QsMetaItemId typeId, Func<QsValue> defaultValueFactory)
        {
            TypeId = typeId;
            this.defaultValueFactory = defaultValueFactory;
        }

        public QsTypeInst Instantiate(QsDomainService domainService, QsTypeValue_Normal ntv)
        {
            return new QsNativeTypeInst(ntv, defaultValueFactory);
        }
    }
}