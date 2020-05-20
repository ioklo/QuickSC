using QuickSC;
using QuickSC.Runtime;
using System;
using Xunit;

namespace QuickSC.RuntimeModule.Test
{
    public class UnitTest1
    {
        [Fact]
        static void Temp()
        {
            QsRuntimeModule runtimeModule = new QsRuntimeModule();

            // List<int>�� ����� ����
            if (!runtimeModule.GetGlobalType("int", 0, out var intType))
                return;

            if (!runtimeModule.GetGlobalType("List", 1, out var listType))
                return;

            if (!listType.GetMemberFuncId(new QsFuncName("Add"), out var funcInfo))
                return;

            // outer�����ϴ� �κ��� ������ ����⿡�� �Ǽ��� �κ��� ���� �� ����
            var listIntTypeValue = new QsNormalTypeValue(null, listType.TypeId, new QsNormalTypeValue(null, intType.TypeId));
            var listIntAddFuncValue = new QsFuncValue(listIntTypeValue, funcInfo.Value.FuncId);

            var funcInst = runtimeModule.GetFuncInst(listIntAddFuncValue) as QsNativeFuncInst;
            funcInst.CallAsync(thisValue, args);

            // FuncValue�� ����� ���� 

        }
    }
}
