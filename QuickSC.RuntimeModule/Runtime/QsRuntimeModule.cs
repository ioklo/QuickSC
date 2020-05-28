using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    using Invoker = Func<ImmutableArray<QsTypeInst>, QsValue?, ImmutableArray<QsValue>, ValueTask<QsValue>>;

    public class QsRuntimeModule : IQsRuntimeModule, IQsMetadata
    {
        public const string MODULE_NAME = "System.Runtime";
        public string ModuleName => MODULE_NAME;

        // TODO: localId와 globalId를 나눠야 할 것 같다. 내부에서는 LocalId를 쓰고, Runtime은 GlobalId로 구분해야 할 것 같다

        // globalTypes
        ImmutableDictionary<QsTypeId, QsType> typesById;        
        ImmutableDictionary<QsFuncId, (QsFunc Func, Invoker Invoker)> funcsById;

        Dictionary<QsTypeId, QsValue> defaultValuesByTypeId;

        QsType MakeEmptyGlobalType(QsTypeId typeId)
        {
            return new QsDefaultType(
                typeId,
                ImmutableArray<string>.Empty,
                null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                ImmutableDictionary<QsName, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty);
        }

        public QsRuntimeModule()
        {
            var typeBuilder = new QsTypeBuilder(this);

            var boolType = typeBuilder.AddType(MakeEmptyGlobalType(new QsTypeId(MODULE_NAME, new QsNameElem("bool", 0))), new QsValue<bool>(false));
            var intType = typeBuilder.AddType(MakeEmptyGlobalType(new QsTypeId(MODULE_NAME, new QsNameElem("int", 0))), new QsValue<int>(0));
            typeBuilder.AddType(MakeEmptyGlobalType(new QsTypeId(MODULE_NAME, new QsNameElem("string", 0))), new QsObjectValue(null));

            var enumeratorType = QsAsyncEnumeratorObject.AddType(typeBuilder, new QsNormalTypeValue(null, boolType.TypeId));
            QsListObject.AddType(enumeratorType.TypeId, new QsNormalTypeValue(null, intType.TypeId), typeBuilder);

            typesById = typeBuilder.GetAllTypes();
            funcsById = typeBuilder.GetAllFuncs();

            defaultValuesByTypeId = new Dictionary<QsTypeId, QsValue>();
        }
        
        public string GetString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다
            throw new InvalidOperationException();
        }

        public QsValue MakeAsyncEnumerable(IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            return new QsObjectValue(new QsAsyncEnumerableObject(asyncEnumerable));
        }

        public QsValue MakeListObject(List<QsValue> elems)
        {
            return new QsObjectValue(new QsListObject(elems));
        }

        public bool GetTypeById(QsTypeId typeId, [NotNullWhen(true)] out QsType? outType)
        {
            return typesById.TryGetValue(typeId, out outType);
        }

        public bool GetFuncById(QsFuncId funcId, [NotNullWhen(true)] out QsFunc? outFunc)
        {
            outFunc = null;
            return false;
        }

        public bool GetVarById(QsVarId typeId, [NotNullWhen(true)] out QsVariable? outVar)
        {
            outVar = null;
            return false;
        }

        public QsValue MakeBool(bool b)
        {
            return new QsValue<bool>(b);
        }

        public QsValue MakeInt(int i)
        {
            return new QsValue<int>(i);
        }

        public QsValue MakeString(string str)
        {
            return new QsObjectValue(new QsStringObject(str));
        }

        public QsValue MakeList(List<QsValue> elems)
        {
            return new QsObjectValue(new QsListObject(elems));
        }

        public int GetInt(QsValue value)
        {
            return ((QsValue<int>)value).Value;
        }

        public void SetInt(QsValue value, int i)
        {
            ((QsValue<int>)value).Value = i;
        }

        public bool GetBool(QsValue value)
        {
            return ((QsValue<bool>)value).Value;
        }

        public void SetBool(QsValue value, bool b)
        {
            ((QsValue<bool>)value).Value = b;
        }

        public QsFuncInst GetFuncInst(QsFuncId funcId, ImmutableArray<QsTypeInst> typeArgs)
        {
            // X<T(X)>.Y<U(Y)>.Func<V(F)>()
            // X<int>.Y<short>.Func<bool>() 를 만들어 봅시다. typeEnv를 만들어서 그냥 던져 볼겁니다
            var func = funcsById[funcId];            // TODO: funcsById 이름 변경할 것
            var Invoker = func.Invoker;

            // typeEnv는 [T(X) => int, U(Y) => short, V(F) => bool]
            return new QsNativeFuncInst(func.Func.bThisCall, (thisValue, argValues) => Invoker(typeArgs, thisValue, argValues));
        }

        public QsTypeInst GetTypeInst(QsTypeId typeId, ImmutableArray<QsTypeInst> typeArgs)
        {
            return new QsRawTypeInst(typesById[typeId], defaultValuesByTypeId[typeId], typeArgs);
        }        
    }
}
