using QuickSC;
using QuickSC.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Xunit;

namespace QuickSC.RuntimeModule.Test
{
    public class UnitTest1
    {
        [Fact]
        static void Temp()
        {
            var runtimeModule = new QsRuntimeModule(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Directory.GetCurrentDirectory());

            QsMetadataService metadataService = new QsMetadataService(ImmutableArray.Create<IQsMetadata>(runtimeModule));

            QsDomainService domainService = new QsDomainService();

            domainService.LoadModule(runtimeModule);

            var intTypeId = QsRuntimeModule.IntId;
            var listTypeId = QsRuntimeModule.ListId;

            // int
            var intTypeValue = QsTypeValue.MakeNormal(intTypeId, QsTypeArgumentList.Empty);           
            

            // List<int>.Add
            var listAddFuncId = listTypeId.Append("Add", 0);
            var listAddFuncTypeArgList = QsTypeArgumentList.Make(new[] { intTypeValue }, new QsTypeValue[] { }); // ù��°�� List, �ι�°�� Add�� ���� typeArgs
            var funcInst = domainService.GetFuncInst(new QsFuncValue(listAddFuncId, listAddFuncTypeArgList));
            

            // list = [1, 2]
            var list = runtimeModule.MakeList(domainService, intTypeValue, new List<QsValue> { runtimeModule.MakeInt(1), runtimeModule.MakeInt(2) });

            // List<int>.Add(list, 3)
            if( funcInst is QsNativeFuncInst nativeFuncInst )
                nativeFuncInst.CallAsync(list, ImmutableArray.Create<QsValue>(runtimeModule.MakeInt(3)), QsVoidValue.Instance);

            // [1, 2, 3]
            Assert.True(list is QsObjectValue objValue && 
                objValue.Object is QsListObject lstObj &&
                runtimeModule.GetInt(lstObj.Elems[0]) == 1 &&
                runtimeModule.GetInt(lstObj.Elems[1]) == 2 &&
                runtimeModule.GetInt(lstObj.Elems[2]) == 3);

            // List<int>
            //var listIntTypeValue = QsTypeValue.MakeNormal(null, listType.TypeId, QsTypeValue.MakeNormal(null, intType.TypeId));
            //var listIntAddFuncValue = new QsFuncValue(listIntTypeValue, funcInfo.Value.FuncId);

            //// List<T>.Add
            //// (List<>.Add), (T(List) => int)

            //// ���� TypeValue�� TypeInst�� ������ֳ�.. DomainService
            //var typeInstEnv = domainService.MakeTypeInstEnv(listIntAddFuncValue);

            //var funcInst = domainService.GetFuncInst(listIntAddFuncValue) as QsNativeFuncInst;

            // FuncValue�� ����� ���� 
        }
    }
}
