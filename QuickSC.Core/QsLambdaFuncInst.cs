using QuickSC.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QuickSC
{
    internal class QsLambdaFuncInst : QsFuncInst
    {
        public override bool bStatic { get; }
        QsEvaluator evaluator;
        QsEvalContext evalContext; // 새 프레임에 캡쳐가 들어간 상태
        QsLambdaExp exp;

        public QsLambdaFuncInst(bool bStatic, QsEvaluator evaluator, QsEvalContext context, QsLambdaExp exp)
        {
            this.bStatic = bStatic;
            this.evaluator = evaluator;
            this.evalContext = context;
            this.exp = exp;
        }

        public override async ValueTask<QsValue> CallAsync(ImmutableArray<QsValue> args)
        {
            Debug.Assert(exp.Params.Length == args.Length);

            for (int i = 0; i < args.Length; i++)
                evalContext.SetLocalVar(exp.Params[i].Name, args[i]);

            await foreach (var _ in evaluator.EvaluateStmtAsync(exp.Body, evalContext)) { }

            if (evalContext.FlowControl is QsReturnEvalFlowControl returnFlowControl)
                return returnFlowControl.Value;
            else
                return QsVoidValue.Instance;
        }
    }
}