using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    using Invoker = Func<DomainService, TypeArgumentList, Value?, IReadOnlyList<Value>, Value, ValueTask>;

    class QsEnumerableBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsEnumerableBuildInfo()
            : base(null, QsRuntimeModule.EnumerableId, ImmutableArray.Create("T"), null, () => new ObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {            
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            // T
            var enumerableId = QsRuntimeModule.EnumerableId;
            var elemTypeValue = TypeValue.MakeTypeVar(enumerableId, "T");

            // Enumerator<T>
            var enumeratorTypeValue = TypeValue.MakeNormal(enumeratorId, TypeArgumentList.Make(elemTypeValue));

            var funcIdsBuilder = ImmutableArray.CreateBuilder<ModuleItemId>();

            Invoker wrappedGetEnumerator = 
                (domainService, typeArgs, thisValue, args, result) => QsEnumerableObject.NativeGetEnumerator(domainService, enumeratorId, typeArgs, thisValue, args, result);

            builder.AddMemberFunc(Name.MakeText("GetEnumerator"),
                false, true, ImmutableArray<string>.Empty, enumeratorTypeValue, ImmutableArray<TypeValue>.Empty,
                wrappedGetEnumerator);
        }
        
    }

    class QsEnumerableObject : GumObject
    {
        TypeInst typeInst;
        IAsyncEnumerable<Value> enumerable;

        public QsEnumerableObject(TypeInst typeInst, IAsyncEnumerable<Value> enumerable)
        {
            this.typeInst = typeInst;
            this.enumerable = enumerable;
        }
        
        // Enumerator<T> Enumerable<T>.GetEnumerator()
        internal static ValueTask NativeGetEnumerator(DomainService domainService, ModuleItemId enumeratorId, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumerableObject = GetObject<QsEnumerableObject>(thisValue);

            var enumeratorInst = domainService.GetTypeInst(TypeValue.MakeNormal(enumeratorId, typeArgList)); // 같은 TypeArgList를 사용한다
            
            ((ObjectValue)result).SetObject(new QsEnumeratorObject(enumeratorInst, enumerableObject.enumerable.GetAsyncEnumerator()));

            return new ValueTask();
        }

        public override TypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
