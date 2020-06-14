using System;

namespace QuickSC.Runtime
{
    public class QsNativeTypeInstantiator
    {
        Func<QsValue> defaultValueFactory;

        public QsNativeTypeInstantiator(Func<QsValue> defaultValueFactory)
        {
            this.defaultValueFactory = defaultValueFactory;
        }

        public QsTypeInst Instantiate(QsDomainService domainService, QsNormalTypeValue ntv)
        {
            // class X<T> { class Y<U> : B<U, T> { } }
            // 
            // GetTypeInst(domainService, X<>.Y<>, [intInst, boolInst])
            //     GetTypeInst(domainService, B<,>, [boolInst, intInst])

            if (!domainService.GetBaseTypeValue(ntv, out var baseTypeValue))
                throw new InvalidOperationException();

            QsTypeInst? baseTypeInst = null;
            if (baseTypeValue != null)
                baseTypeInst = domainService.GetTypeInst(baseTypeValue);

            var typeEnv = domainService.MakeTypeEnv(ntv);            

            return new QsNativeTypeInst(ntv, baseTypeInst, ntv.TypeId, defaultValueFactory, typeEnv);
        }
    }
}