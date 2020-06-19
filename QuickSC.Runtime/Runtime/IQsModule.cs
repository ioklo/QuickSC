using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsModuleTypeInfo
    {
        QsMetaItemId TypeId { get; }
        QsTypeInst GetTypeInst(QsDomainService domainService, QsNormalTypeValue typeValue);
    }

    public interface IQsModuleFuncInfo
    {
        QsMetaItemId FuncId { get; }
        QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue);
    }

    public interface IQsModule : IQsMetadata
    {
        IEnumerable<IQsModuleTypeInfo> TypeInfos { get; }
        IEnumerable<IQsModuleFuncInfo> FuncInfos { get; }

        void OnLoad(QsDomainService domainService);
    }
}
