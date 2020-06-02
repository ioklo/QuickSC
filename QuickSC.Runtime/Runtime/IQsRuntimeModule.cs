using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsRuntimeModule : IQsModule
    {
        QsValue MakeBool(bool b);
        QsValue MakeInt(int i);
        QsValue MakeString(QsDomainService domainService, string str);
        QsValue MakeEnumerable(QsDomainService domainService, QsTypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable);
        QsValue MakeList(QsDomainService domainService, QsTypeValue elemTypeValue, List<QsValue> elems);

        int GetInt(QsValue value);
        void SetInt(QsValue value, int i);

        bool GetBool(QsValue value);
        void SetBool(QsValue value, bool b);

        string GetString(QsValue value);
    }
}
