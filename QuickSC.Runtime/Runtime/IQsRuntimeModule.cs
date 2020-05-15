using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    public interface IQsRuntimeModule : IQsMetadata
    {
        QsObject MakeStringObject(string str);
        string? GetString(QsValue value);
        QsObject MakeAsyncEnumerableObject(IAsyncEnumerable<QsValue> asyncEnumerable);
        QsObject MakeListObject(List<QsValue> elems);
    }
}
