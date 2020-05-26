using QuickSC;
using QuickSC.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace QuickSC.RuntimeModule.Test
{
    public class UnitTest1
    {
        [Fact]
        static void Temp()
        {
            QsDomainService domainService = new QsDomainService();

            var runtimeModule = new QsRuntimeModule();
            domainService.AddModule(runtimeModule);

            var intTypeId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("int", 0));
            var listTypeId = new QsTypeId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1));

            // int
            var intTypeInst = domainService.GetTypeInst(intTypeId, ImmutableArray<QsTypeInst>.Empty);

            // List<int>
            var listTypeInst = domainService.GetTypeInst(listTypeId, ImmutableArray.Create(intTypeInst));

            // List<int>.Add
            var listAddFuncId = new QsFuncId(QsRuntimeModule.MODULE_NAME, new QsNameElem("List", 1), new QsNameElem("Add", 0));
            var funcInst = domainService.GetFuncInst(listAddFuncId, ImmutableArray.Create(intTypeInst));

            // list = [1, 2]
            var list = runtimeModule.MakeList(new List<QsValue> { runtimeModule.MakeInt(1), runtimeModule.MakeInt(2) });

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
