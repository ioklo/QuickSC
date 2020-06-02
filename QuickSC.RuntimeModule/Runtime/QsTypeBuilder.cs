using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, QsTypeEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsTypeBuilder
    {
        IQsMetadata metadata;        
        List<QsNativeType> types;
        List<QsNativeFunc> funcs;

        public QsTypeBuilder(IQsMetadata metadata)
        {
            this.metadata = metadata;
            types = new List<QsNativeType>();
            funcs = new List<QsNativeFunc>();
        }

        public QsType AddType(QsType type, QsValue defaultValue)
        {
            types.Add(new QsNativeType(type, defaultValue));
            return type;
        }

        public QsFunc AddFunc(Invoker Invoker, QsFunc func)
        {
            funcs.Add(new QsNativeFunc(func, Invoker));
            return func;
        }        

        public ImmutableDictionary<QsTypeId, QsNativeType> GetAllTypes() =>
            types.ToImmutableDictionary(type => type.Type.TypeId);

        public ImmutableDictionary<QsFuncId, QsNativeFunc> GetAllFuncs() =>
            funcs.ToImmutableDictionary(info => info.Func.FuncId);
    }
}