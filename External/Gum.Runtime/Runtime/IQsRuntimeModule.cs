using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsRuntimeModule : IQsModule
    {
        QsValue MakeBool(bool b);
        QsValue MakeInt(int i);        
        // QsValue MakeEnumerable(QsDomainService domainService, QsTypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable);
        // QsValue MakeList(QsDomainService domainService, QsTypeValue elemTypeValue, List<QsValue> elems);
        QsObjectValue MakeNullObject();

        int GetInt(QsValue value);
        void SetInt(QsValue value, int i);

        bool GetBool(QsValue value);
        void SetBool(QsValue value, bool b);

        string GetString(QsValue value);
        void SetString(QsDomainService domainService, QsValue value, string s);

        void SetList(QsDomainService domainService, QsValue value, TypeValue elemTypeValue, List<QsValue> elems);
        void SetEnumerable(QsDomainService domainService, QsValue value, TypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable);
    }
}
