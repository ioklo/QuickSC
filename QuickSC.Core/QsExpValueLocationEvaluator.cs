using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    // exp로부터 location을 얻어냅니다
    class QsExpValueLocationEvaluator
    {
        QsValue EvalIdExp(QsIdentifierExp idExp, QsEvalContext context)
        {
            var info = (QsIdentifierExpInfo)context.AnalyzeInfo.InfosByNode[idExp];

            switch (info.Storage)
            {
                case QsStorage.ModuleGlobal storage:
                    return context.DomainService.GetGlobalValue(storage.VarId);

                case QsStorage.PrivateGlobal storage:
                    return context.PrivateGlobalVars[storage.Index]!;

                case QsStorage.Local storage:
                    return context.LocalVars[storage.Index]!;

                default:
                    throw new NotImplementedException();
            }
        }

        QsValue EvalIndexerExp(QsIndexerExp indexerExp, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        QsValue EvalMemberExp(QsMemberExp memberExp, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public QsValue Eval(QsExp exp, QsEvalContext context)
        {
            switch(exp)
            {
                // a = 3;
                case QsIdentifierExp idExp: 
                    return EvalIdExp(idExp, context);

                // a["h"] = 3;
                case QsIndexerExp indexerExp:
                    return EvalIndexerExp(indexerExp, context);

                // a.x = 3;
                case QsMemberExp memberExp:
                    return EvalMemberExp(memberExp, context);

                // location이 없는 것들
                case QsBoolLiteralExp boolExp: 
                case QsIntLiteralExp intExp:
                case QsStringExp stringExp:
                case QsUnaryOpExp unaryOpExp:
                case QsBinaryOpExp binaryOpExp: 
                case QsCallExp callExp: 
                case QsLambdaExp lambdaExp:
                case QsMemberCallExp memberCallExp:
                case QsListExp listExp:
                default:
                    throw new InvalidOperationException();
            };
        }
    }
}
