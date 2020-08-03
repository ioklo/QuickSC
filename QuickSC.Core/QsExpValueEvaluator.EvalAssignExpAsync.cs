using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    partial class QsExpValueEvaluator
    {
        private ValueTask EvalAssignExpAsync(QsBinaryOpExp exp, QsValue result, QsEvalContext context)
        {
            // 내부함수는 꼭 필요하지 않은 이상 한 단계 아래로 만들지 않기로
            
            // x = 3, e.m = 3, C.m = 3, ...
            async ValueTask EvalDirectAsync(QsBinaryOpExpAssignInfo.Direct directInfo)
            {
                var loc = await GetValueAsync(directInfo.StorageInfo, context);
                await evaluator.EvalExpAsync(exp.Operand1, loc, context);
                result.SetValue(loc);
            }            

            // property, indexer
            async ValueTask EvalCallSetterAsync(QsBinaryOpExpAssignInfo.CallSetter callSetterInfo)
            {
                QsValue? thisValue = null;
                
                // 1. Object 부분 실행
                if (callSetterInfo.Object != null)
                {
                    thisValue = evaluator.GetDefaultValue(callSetterInfo.ObjectTypeValue!, context);
                    await evaluator.EvalExpAsync(callSetterInfo.Object, thisValue, context);
                }

                var argValues = new List<QsValue>(callSetterInfo.Arguments.Length);

                // 2. operand1 실행
                var value = evaluator.GetDefaultValue(callSetterInfo.ValueTypeValue, context);
                await evaluator.EvalExpAsync(exp.Operand1, value, context);

                // 3. set value 호출 Object.Setter(Operand1)
                foreach (var arg in callSetterInfo.Arguments)
                {
                    var argValue = evaluator.GetDefaultValue(arg.TypeValue, context);
                    await evaluator.EvalExpAsync(arg.Exp, argValue, context);
                    argValues.Add(argValue);
                }

                argValues.Add(value);

                var setterInst = context.DomainService.GetFuncInst(callSetterInfo.Setter);
                await evaluator.EvaluateFuncInstAsync(thisValue, setterInst, argValues.ToImmutableArray(), QsVoidValue.Instance, context);

                // 4. result는 operand1실행 결과
                result.SetValue(value);
            }

            // BODY
            var info = context.GetNodeInfo<QsBinaryOpExpAssignInfo>(exp);

            if (info is QsBinaryOpExpAssignInfo.Direct directInfo)
                return EvalDirectAsync(directInfo);
            else if (info is QsBinaryOpExpAssignInfo.CallSetter callSetterInfo)
                return EvalCallSetterAsync(callSetterInfo);
            else
                throw new InvalidOperationException();
        }
    }
}
