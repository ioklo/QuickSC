using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace QuickSC
{
    public struct QsEnumElemFieldInfo
    {
        public QsTypeValue TypeValue { get; }
        public string Name { get; }

        public QsEnumElemFieldInfo(QsTypeValue typeValue, string name)
        {
            TypeValue = typeValue;
            Name = name;
        }
    }

    public struct QsEnumElemInfo
    {
        public string Name { get; }
        public ImmutableArray<QsEnumElemFieldInfo> FieldInfos { get; }

        public QsEnumElemInfo(string name, IEnumerable<QsEnumElemFieldInfo> fieldInfos)
        {
            Name = name;
            FieldInfos = fieldInfos.ToImmutableArray();
        }
    }

    public interface IQsEnumInfo : IQsTypeInfo
    {
        bool GetElemInfo(string idName, [NotNullWhen(returnValue: true)] out QsEnumElemInfo? outElemInfo);
        QsEnumElemInfo GetDefaultElemInfo();
    }
}