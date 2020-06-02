using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    public class QsRuntimeModule : IQsRuntimeModule, IQsMetadata
    {
        public const string MODULE_NAME = "System.Runtime";
        public string ModuleName => MODULE_NAME;

        private QsType enumerableType;
        private QsType enumeratorType;
        private QsType listType;
        private QsType stringType;

        // TODO: localId와 globalId를 나눠야 할 것 같다. 내부에서는 LocalId를 쓰고, Runtime은 GlobalId로 구분해야 할 것 같다

        // globalTypes
        ImmutableDictionary<QsTypeId, QsNativeType> nativeTypesById;
        ImmutableDictionary<QsFuncId, QsNativeFunc> nativeFuncsById;

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

            stringType = typeBuilder.AddType(MakeEmptyGlobalType(new QsTypeId(MODULE_NAME, new QsNameElem("string", 0))), new QsObjectValue(null));
            enumeratorType = QsEnumeratorObject.AddType(typeBuilder, new QsNormalTypeValue(null, boolType.TypeId));
            listType = QsListObject.AddType(typeBuilder, enumeratorType.TypeId, new QsNormalTypeValue(null, intType.TypeId));

            enumerableType = QsEnumerableObject.AddType(typeBuilder, enumeratorType.TypeId);

            nativeTypesById = typeBuilder.GetAllTypes();
            nativeFuncsById = typeBuilder.GetAllFuncs();
        }
        
        public string GetString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다
            throw new InvalidOperationException();
        }

        public QsValue MakeEnumerable(QsDomainService domainService, QsTypeValue elemTypeValue, IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            var enumerableInst = domainService.GetTypeInst(new QsNormalTypeValue(null, enumerableType.TypeId, elemTypeValue));

            return new QsObjectValue(new QsEnumerableObject(enumerableInst, asyncEnumerable));
        }
        
        public bool GetTypeById(QsTypeId typeId, [NotNullWhen(true)] out QsType? outType)
        {
            if (!nativeTypesById.TryGetValue(typeId, out var nativeType))
            {
                outType = null;
                return false;
            }

            outType = nativeType.Type;
            return true;
        }

        public bool GetFuncById(QsFuncId funcId, [NotNullWhen(true)] out QsFunc? outFunc)
        {
            if (!nativeFuncsById.TryGetValue(funcId, out var nativeFunc))
            {
                outFunc = null;
                return false;
            }

            outFunc = nativeFunc.Func;
            return true;
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

        public QsValue MakeString(QsDomainService domainService, string str)
        {
            var stringInst = domainService.GetTypeInst(new QsNormalTypeValue(null, stringType.TypeId));
            return new QsObjectValue(new QsStringObject(stringInst, str));
        }

        public QsValue MakeList(QsDomainService domainService, QsTypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(new QsNormalTypeValue(null, listType.TypeId, elemTypeValue));

            return new QsObjectValue(new QsListObject(listInst, elems));
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

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue fv)
        {
            var typeEnv = domainService.MakeTypeEnv(fv);

            // X<T(X)>.Y<U(Y)>.Func<V(F)>()
            // X<int>.Y<short>.Func<bool>() 를 만들어 봅시다. typeEnv를 만들어서 그냥 던져 볼겁니다
            var func = nativeFuncsById[fv.FuncId];            // TODO: funcsById 이름 변경할 것
            var Invoker = func.Invoker;

            // typeEnv는 [T(X) => int, U(Y) => short, V(F) => bool]
            return new QsNativeFuncInst(func.Func.bThisCall, (thisValue, argValues) => Invoker(domainService, typeEnv, thisValue, argValues));
        }
        
        public QsTypeInst GetTypeInst(QsDomainService domainService, QsNormalTypeValue ntv)
        {
            // class X<T> { class Y<U> : B<U, T> { } }
            // 
            // GetTypeInst(domainService, X<>.Y<>, [intInst, boolInst])
            //     GetTypeInst(domainService, B<,>, [boolInst, intInst])

            if (!domainService.GetBaseTypeValue(ntv, out var baseTypeValue))
                throw new InvalidOperationException();

            QsTypeInst? baseTypeInst = null;
            if (baseTypeValue != null)
                baseTypeInst = domainService.GetTypeInst(baseTypeValue);

            var typeEnv = domainService.MakeTypeEnv(ntv);

            var nativeType = nativeTypesById[ntv.TypeId];

            return new QsNativeTypeInst(baseTypeInst, ntv.TypeId, nativeType.DefaultValue, typeEnv);
        }
    }
}
