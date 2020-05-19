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
                ImmutableDictionary<string, QsVarId>.Empty,
                ImmutableDictionary<QsMemberFuncId, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty);
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

        public bool GetGlobalFunc(string name, [NotNullWhen(true)] out QsFunc? func)
        {
            func = null;
            return false;
        }

        public bool GetGlobalVar(string name, [NotNullWhen(true)] out QsVariable? outVar)
        {
            outVar = null;
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
        

        public bool GetTypeById(QsTypeId typeId, [NotNullWhen(true)] out QsType? outType)
        {
            if (typeId == boolType.TypeId)
            {
                outType = boolType;
                return true;
            }
            else if (typeId == intType.TypeId)
            {
                outType = intType;
                return true;
            }
            else if (typeId == stringType.TypeId)
            {
                outType = stringType;
                return true;
            }
            else
            {
                outType = null;
                return false;
            }
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
    }
}
