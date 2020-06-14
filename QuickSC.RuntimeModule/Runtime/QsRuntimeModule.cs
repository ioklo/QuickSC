using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC.Runtime
{
    public class QsNativeModuleTypeInfo : IQsModuleTypeInfo
    {
        public QsMetaItemId TypeId { get; }
        private QsNativeTypeInstantiator instantiator;
        
        public QsNativeModuleTypeInfo(QsMetaItemId typeId, QsNativeTypeInstantiator instantiator)
        {
            TypeId = typeId;
            this.instantiator = instantiator;
        }

        public QsTypeInst GetTypeInst(QsDomainService domainService, QsNormalTypeValue typeValue)
            => instantiator.Instantiate(domainService, typeValue);
    }

    public class QsNativeModuleFuncInfo : IQsModuleFuncInfo
    {
        public QsMetaItemId FuncId { get; }
        private QsNativeFuncInstantiator instantiator;

        public QsNativeModuleFuncInfo(QsMetaItemId funcId, QsNativeFuncInstantiator instantiator)
        {
            FuncId = funcId;
            this.instantiator = instantiator;
        }

        public QsFuncInst GetFuncInst(QsDomainService domainService, QsFuncValue funcValue)
            => instantiator.Instantiate(domainService, funcValue);
    }
    
    public class QsRuntimeModule : IQsRuntimeModule
    {
        public string ModuleName { get; }
        public IEnumerable<IQsModuleTypeInfo> TypeInfos { get; }
        public IEnumerable<IQsModuleFuncInfo> FuncInfos { get; }

        public QsRuntimeModule(
            string moduleName,
            IEnumerable<IQsModuleTypeInfo> typeInfos, 
            IEnumerable<IQsModuleFuncInfo> funcInfos)
        {
            ModuleName = moduleName;            
            TypeInfos = typeInfos;
            FuncInfos = funcInfos;
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
            var enumerableInst = domainService.GetTypeInst(new QsNormalTypeValue(null, QsRuntimeModuleInfo.EnumerableId, elemTypeValue));
            return new QsObjectValue(new QsEnumerableObject(enumerableInst, asyncEnumerable));
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
            var stringInst = domainService.GetTypeInst(new QsNormalTypeValue(null, QsRuntimeModuleInfo.StringId));
            return new QsObjectValue(new QsStringObject(stringInst, str));
        }

        public QsValue MakeList(QsDomainService domainService, QsTypeValue elemTypeValue, List<QsValue> elems)
        {
            var listInst = domainService.GetTypeInst(new QsNormalTypeValue(null, QsRuntimeModuleInfo.ListId, elemTypeValue));

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
    }
}
