using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsRuntimeModule : IQsModule
    {
        QsValue MakeBool(bool b);
        QsValue MakeInt(int i);
        QsValue MakeString(string str);
        QsValue MakeEnumerable(QsTypeInst elemTypeInst, IAsyncEnumerable<QsValue> asyncEnumerable);
        QsValue MakeList(QsTypeInst elemTypeInst, List<QsValue> elems);

        int GetInt(QsValue value);
        void SetInt(QsValue value, int i);

        bool GetBool(QsValue value);
        void SetBool(QsValue value, bool b);

        string GetString(QsValue value);
    }
}
