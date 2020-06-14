using QuickSC;
using QuickSC.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace QuickSC.RuntimeModule.Test
{
    public class UnitTest1
    {
        class QsTestErrorCollector : IQsErrorCollector
        {
            public bool HasError => throw new NotImplementedException();

            public void Add(object obj, string msg)
            {
                throw new NotImplementedException();
            }
        }


        [Fact]
        static void Temp()
        {
            var errorCollector = new QsTestErrorCollector();

            var runtimeModuleInfo = new QsRuntimeModuleInfo();

            QsMetadataService metadataService = new QsMetadataService(
                ImmutableArray<QsType>.Empty, 
                ImmutableArray<QsFunc>.Empty, 
                ImmutableArray<QsVariable>.Empty,
                ImmutableArray.Create<IQsMetadata>(runtimeModuleInfo.GetMetadata()));

            QsDomainService domainService = new QsDomainService(metadataService);

            var runtimeModule = runtimeModuleInfo.MakeRuntimeModule(domainService);

            var intTypeId = QsRuntimeModuleInfo.IntId;
            var listTypeId = QsRuntimeModuleInfo.ListId;

            // int
            var intTypeValue = new QsNormalTypeValue(null, intTypeId);

            // List<int>
            var listTypeValue = new QsNormalTypeValue(null, listTypeId, intTypeValue);

            // List<int>.Add
            var listAddFuncId = listTypeId.Append(new QsMetaItemIdElem("Add", 0));
            var funcInst = domainService.GetFuncInst(new QsFuncValue(listTypeValue, listAddFuncId));
            

            // list = [1, 2]
            var list = runtimeModule.MakeList(domainService, intTypeValue, new List<QsValue> { runtimeModule.MakeInt(1), runtimeModule.MakeInt(2) });

            // List<int>.Add(list, 3)
            if( funcInst is QsNativeFuncInst nativeFuncInst )
                nativeFuncInst.CallAsync(list, ImmutableArray.Create<QsValue>(runtimeModule.MakeInt(3)));

            // [1, 2, 3]
            Assert.True(list is QsObjectValue objValue && 
                objValue.Object is QsListObject lstObj &&
                runtimeModule.GetInt(lstObj.Elems[0]) == 1 &&
                runtimeModule.GetInt(lstObj.Elems[1]) == 2 &&
                runtimeModule.GetInt(lstObj.Elems[2]) == 3);

            // List<int>
            //var listIntTypeValue = new QsNormalTypeValue(null, listType.TypeId, new QsNormalTypeValue(null, intType.TypeId));
            //var listIntAddFuncValue = new QsFuncValue(listIntTypeValue, funcInfo.Value.FuncId);

            //// List<T>.Add
            //// (List<>.Add), (T(List) => int)

            //// 누가 TypeValue를 TypeInst로 만들어주나.. DomainService
            //var typeInstEnv = domainService.MakeTypeInstEnv(listIntAddFuncValue);

            //var funcInst = domainService.GetFuncInst(listIntAddFuncValue) as QsNativeFuncInst;

            // FuncValue를 만들어 보자 
        }
    }
}
