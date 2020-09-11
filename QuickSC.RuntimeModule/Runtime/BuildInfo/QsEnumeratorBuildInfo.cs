using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Runtime
{
    class QsEnumeratorBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsEnumeratorBuildInfo()
            : base(null, QsRuntimeModule.EnumeratorId, ImmutableArray.Create("T"), null, () => new ObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            var enumeratorId = QsRuntimeModule.EnumeratorId;

            var funcIdsBuilder = ImmutableArray.CreateBuilder<ModuleItemId>();
            var elemTypeValue = TypeValue.MakeTypeVar(enumeratorId, "T");
            var boolTypeValue = TypeValue.MakeNormal(QsRuntimeModule.BoolId);

            // bool Enumerator<T>.MoveNext()
            builder.AddMemberFunc(
                Name.MakeText("MoveNext"),
                false, true,
                ImmutableArray<string>.Empty, boolTypeValue, ImmutableArray<TypeValue>.Empty,
                QsEnumeratorObject.NativeMoveNext);

            // T Enumerator<T>.GetCurrent()
            builder.AddMemberFunc(Name.MakeText("GetCurrent"),
                false, true,
                ImmutableArray<string>.Empty, elemTypeValue, ImmutableArray<TypeValue>.Empty,
                QsEnumeratorObject.NativeGetCurrent);
        }
    }

    class QsEnumeratorObject : GumObject
    {
        TypeInst typeInst;
        IAsyncEnumerator<Value> enumerator;

        public QsEnumeratorObject(TypeInst typeInst, IAsyncEnumerator<Value> enumerator)
        {
            this.typeInst = typeInst;
            this.enumerator = enumerator;
        }

        internal static async ValueTask NativeMoveNext(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);

            bool bResult = await enumeratorObj.enumerator.MoveNextAsync();

            ((Value<bool>)result).Data = bResult;
        }

        internal static ValueTask NativeGetCurrent(DomainService domainService, TypeArgumentList typeArgList, Value? thisValue, IReadOnlyList<Value> args, Value result)
        {
            Debug.Assert(thisValue != null);
            Debug.Assert(result != null);

            var enumeratorObj = GetObject<QsEnumeratorObject>(thisValue);            
            result.SetValue(enumeratorObj.enumerator.Current);

            return new ValueTask();
        }

        public override TypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
