using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsTypeInstEnv, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsTypeBuilder
    {
        IQsMetadata metadata;
        List<QsType> globalTypes;
        List<QsType> types;        
        List<(QsFunc Func, Invoker Invoker)> funcs;

        public QsTypeBuilder(IQsMetadata metadata)
        {
            this.metadata = metadata;
            globalTypes = new List<QsType>();
            types = new List<QsType>();            
            funcs = new List<(QsFunc Func, Invoker Invoker)>();
        }

        public QsType AddGlobalType(Func<QsTypeId, QsType> Constructor)
        {
            var typeId = new QsTypeId(metadata, types.Count);
            var type = Constructor(typeId);

            types.Add(type);
            globalTypes.Add(type);
            return type;
        }

        public QsFunc AddFunc(Invoker Invoker, Func<QsFuncId, QsFunc> Constructor)
        {
            var funcId = new QsFuncId(metadata, funcs.Count);
            var func = Constructor(funcId);
            funcs.Add((func, Invoker));
            return func;
        }

        public ImmutableDictionary<(string Name, int TypeParamCount), QsType> GetGlobalTypes() =>
            globalTypes.ToImmutableDictionary(type => (type.GetName(), type.GetTypeParams().Length));

        public ImmutableDictionary<QsTypeId, QsType> GetAllTypes() =>
            types.ToImmutableDictionary(type => type.TypeId);

        public ImmutableDictionary<QsFuncId, (QsFunc Func, Invoker Invoker)> GetAllFuncs() =>
            funcs.ToImmutableDictionary(info => info.Func.FuncId);
    }
}