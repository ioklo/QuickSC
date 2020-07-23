using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModule : IQsMetadata
    {
        void OnLoad(QsDomainService domainService);

        QsTypeInst GetTypeInst(QsDomainService domainService, QsTypeValue_Normal typeValue);
        QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue);
    }
}
