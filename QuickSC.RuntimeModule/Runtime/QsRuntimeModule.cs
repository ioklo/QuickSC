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

        QsType boolType;
        QsType intType;
        QsType stringType;

        QsType MakeEmptyGlobalType(string name, int typeIdValue)
        {
            return new QsDefaultType(
                new QsTypeId(this, typeIdValue),
                name,
                ImmutableArray<string>.Empty,
                null,
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsTypeValue>.Empty,
                ImmutableDictionary<QsMemberFuncId, QsFuncId>.Empty,
                ImmutableDictionary<string, QsTypeValue>.Empty);
        }

        public QsRuntimeModule()
        {
            boolType = MakeEmptyGlobalType("bool", BoolTypeId);
            intType = MakeEmptyGlobalType("int", IntTypeId);
            stringType = MakeEmptyGlobalType("string", StringTypeId);
            
            // List

            //var boolValueType = new QsNormalTypeValue(null, boolType.TypeId);
            //var intValueType = new QsNormalTypeValue(null, intType.TypeId);
            //var stringValueType = new QsNormalTypeValue(null, stringType.TypeId);
            //var listTypeId = globalTypes["list"].TypeId;

            // TODO: list
            // var listType = new QsDefaultType(typeIdFactory.MakeTypeId(), "List",
            //    emptyStrings, null, emptyTypeIds, emptyFuncIds, emptyTypeValuesDict, emptyMemberFuncIds, emptyTypeValuesDict);

        }

        public bool GetGlobalType(string name, int typeParamCount, [NotNullWhen(true)] out QsType? type)
        {
            if (typeParamCount == 0)
            {
                if (name == "bool")
                {
                    type = boolType;
                    return true;
                }
                else if (name == "int")
                {
                    type = intType;
                    return true;
                }
                else if (name == "string")
                {
                    type = stringType;
                    return true;
                }
            }

            type = null;
            return false;
        }
        
        public bool GetGlobalFuncTypeValue(string value, ImmutableArray<QsTypeValue> typeArgs, [NotNullWhen(true)] out QsFuncTypeValue? outFuncTypeValue)
        {
            outFuncTypeValue = null;
            return false;
        }

        public bool GetGlobalVarTypeValue(string value, [NotNullWhen(true)] out QsTypeValue? outTypeValue)
        {
            outTypeValue = null;
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
