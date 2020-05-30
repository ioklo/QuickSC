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
        List<QsNativeType> types;
        Dictionary<QsTypeId, QsValue> defaultValues;
        List<(QsFunc Func, Invoker Invoker)> funcs;

        public QsTypeBuilder(IQsMetadata metadata)
        {
            this.metadata = metadata;
            types = new List<QsNativeType>();
            defaultValues = new Dictionary<QsTypeId, QsValue>();
            funcs = new List<(QsFunc Func, Invoker Invoker)>();
        }

        public QsType AddType(QsType type, QsValue defaultValue)
        {
            types.Add(new QsNativeType(type, defaultValue));
            return type;
        }

        public QsFunc AddFunc(Invoker Invoker, QsFunc func)
        {
            funcs.Add((func, Invoker));
            return func;
        }        

        public ImmutableDictionary<QsTypeId, QsNativeType> GetAllTypes() =>
            types.ToImmutableDictionary(type => type.Type.TypeId);

        public ImmutableDictionary<QsTypeId, QsValue> GetDefaultValuesByTypeId() =>
            defaultValues.ToImmutableDictionary();

        public ImmutableDictionary<QsFuncId, (QsFunc Func, Invoker Invoker)> GetAllFuncs() =>
            funcs.ToImmutableDictionary(info => info.Func.FuncId);
    }
}