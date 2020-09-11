using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<QsDomainService, TypeArgumentList, QsValue?, IReadOnlyList<QsValue>, QsValue, ValueTask>;

    class QsEnumerableBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsEnumerableBuildInfo()
            : base(null, QsRuntimeModule.EnumerableId, ImmutableArray.Create("T"), null, () => new QsObjectValue(null))
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

    class QsEnumerableObject : QsObject
    {
        QsTypeInst typeInst;
        IAsyncEnumerable<QsValue> enumerable;

        public QsEnumerableObject(QsTypeInst typeInst, IAsyncEnumerable<QsValue> enumerable)
        {
            this.typeInst = typeInst;
            this.enumerable = enumerable;
        }
        
        // Enumerator<T> Enumerable<T>.GetEnumerator()
        internal static ValueTask NativeGetEnumerator(QsDomainService domainService, ModuleItemId enumeratorId, TypeArgumentList typeArgList, QsValue? thisValue, IReadOnlyList<QsValue> args, QsValue result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumerableObject = GetObject<QsEnumerableObject>(thisValue);

            var enumeratorInst = domainService.GetTypeInst(TypeValue.MakeNormal(enumeratorId, typeArgList)); // 같은 TypeArgList를 사용한다
            
            ((QsObjectValue)result).SetObject(new QsEnumeratorObject(enumeratorInst, enumerableObject.enumerable.GetAsyncEnumerator()));

            return new ValueTask();
        }

        public override QsTypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
