using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<ImmutableArray<QsTypeInst>, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsTypeBuilder
    {
        IQsMetadata metadata;        
        List<QsType> types;
        Dictionary<QsTypeId, QsValue> defaultValues;
        List<(QsFunc Func, Invoker Invoker)> funcs;

        public QsTypeBuilder(IQsMetadata metadata)
        {
            this.metadata = metadata;
            types = new List<QsType>();
            defaultValues = new Dictionary<QsTypeId, QsValue>();
            funcs = new List<(QsFunc Func, Invoker Invoker)>();
        }

        public QsType AddType(QsType type, QsValue defaultValue)
        {
            types.Add(type);
            defaultValues.Add(type.TypeId, defaultValue);
            return type;
        }

        public QsFunc AddFunc(Invoker Invoker, QsFunc func)
        {
            funcs.Add((func, Invoker));
            return func;
        }        

        public ImmutableDictionary<QsTypeId, QsType> GetAllTypes() =>
            types.ToImmutableDictionary(type => type.TypeId);

        public ImmutableDictionary<QsTypeId, QsValue> GetDefaultValuesByTypeId() =>
            defaultValues.ToImmutableDictionary();

        public ImmutableDictionary<QsFuncId, (QsFunc Func, Invoker Invoker)> GetAllFuncs() =>
            funcs.ToImmutableDictionary(info => info.Func.FuncId);
    }
}