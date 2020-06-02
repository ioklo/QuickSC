using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModule
    {
        string ModuleName { get; }
        QsTypeInst GetTypeInst(QsDomainService domainService, QsNormalTypeValue typeValue);
        QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue fv);
    }
}
