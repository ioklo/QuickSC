using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC.Runtime
{
    public class QsRuntimeModule : IQsRuntimeModule
    {
        const int BoolTypeId = 1;
        const int IntTypeId = 2;
        const int StringTypeId = 3;
        const int ListTypeId = 4;

        Dictionary<string, QsType> globalTypes;
        
        void AddEmptyGlobalType(string name, int typeIdValue)
        {
            var type = new QsDefaultType(
                new QsTypeId(this, typeIdValue),
                name,
                ImmutableArray<string>.Empty,
                null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsTypeValue>.Empty,
                ImmutableDictionary<QsMemberFuncId, QsFuncId>.Empty,
                ImmutableDictionary<string, QsTypeValue>.Empty);

            globalTypes.Add(name, type);
        }

        public QsRuntimeModule()
        {
            globalTypes = new Dictionary<string, QsType>();

            AddEmptyGlobalType("bool", BoolTypeId);
            AddEmptyGlobalType("int", IntTypeId);
            AddEmptyGlobalType("string", StringTypeId);
            
            // List

            //var voidValueType = new QsNormalTypeValue(null, voidType.TypeId);
            //var boolValueType = new QsNormalTypeValue(null, boolType.TypeId);
            //var intValueType = new QsNormalTypeValue(null, intType.TypeId);
            //var stringValueType = new QsNormalTypeValue(null, stringType.TypeId);
            //var listTypeId = globalTypes["list"].TypeId;

            // TODO: list
            // var listType = new QsDefaultType(typeIdFactory.MakeTypeId(), "List",
            //    emptyStrings, null, emptyTypeIds, emptyFuncIds, emptyTypeValuesDict, emptyMemberFuncIds, emptyTypeValuesDict);

        }

        public bool GetGlobalTypeValue(string name, ImmutableArray<QsTypeValue> typeArgs, [NotNullWhen(true)] out QsTypeValue? typeValue)
        {
            typeValue = null;

            if (typeArgs.Length == 0)
            {
                if (name == "bool" )
                {
                    typeValue = new QsNormalTypeValue(null, new QsTypeId(this, BoolTypeId));
                    return true;
                }
                else if (name == "int")
                {
                    typeValue = new QsNormalTypeValue(null, new QsTypeId(this, IntTypeId));
                    return true;
                }
                else if (name == "string")
                {
                    typeValue = new QsNormalTypeValue(null, new QsTypeId(this, StringTypeId));
                    return true;
                }
            }

            return false;
        }

        public string? GetString(QsValue value)
        {
            if (value is QsObjectValue objValue && objValue.Object is QsStringObject strObj) return strObj.Data;
            if (value is QsValue<int> intValue) return intValue.Value.ToString();
            if (value is QsValue<bool> boolValue) return boolValue.Value ? "true" : "false";

            // TODO: ObjectValue의 경우 ToString()을 찾는다
            return null;
        }

        public QsObject MakeAsyncEnumerableObject(IAsyncEnumerable<QsValue> asyncEnumerable)
        {
            return new QsAsyncEnumerableObject(asyncEnumerable);
        }

        public QsObject MakeListObject(List<QsValue> elems)
        {
            return new QsListObject(elems);
        }

        public QsObject MakeStringObject(string str)
        {
            return new QsStringObject(str);
        }
    }
}
