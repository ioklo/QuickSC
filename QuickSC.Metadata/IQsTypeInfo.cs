using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC
{
    public interface IQsTypeInfo
    {
        QsMetaItemId? OuterTypeId { get; }
        QsMetaItemId TypeId { get; }
        
        IReadOnlyList<string> GetTypeParams();
        QsTypeValue? GetBaseTypeValue();

        // TODO: 셋은 같은 이름공간을 공유한다. 서로 이름이 같은 것이 나오면 안된다 (체크하자)
        bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsMetaItemId? outTypeId);
        bool GetMemberFuncId(QsName memberFuncId, [NotNullWhen(returnValue: true)] out QsMetaItemId? outFuncId);
        bool GetMemberVarId(QsName name, [NotNullWhen(returnValue: true)] out QsMetaItemId? outVarId);
    }
}
