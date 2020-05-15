using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Runtime;
using QuickSC.Syntax;

namespace QuickSC
{
    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        QsExpEvaluator expEvaluator;
        QsCallableEvaluator callableEvaluator;
        QsStmtEvaluator stmtEvaluator;

        internal static bool Eval<TValue>(QsEvalResult<TValue> result, ref QsEvalContext context, out TValue? value) where TValue : class
        {
            if (!result.HasValue)
            {
                value = null;
                return false;
            }

            context = result.Context;
            value = result.Value;
            return true;
        }

        public QsEvaluator(IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {            
            this.expEvaluator = new QsExpEvaluator(this, runtimeModule);
            this.stmtEvaluator = new QsStmtEvaluator(this, expEvaluator, commandProvider, runtimeModule);

            this.callableEvaluator = new QsCallableEvaluator(stmtEvaluator, runtimeModule);
        }

        internal ValueTask<QsEvalResult<QsValue>> EvaluateCallableAsync(QsCallable callable, QsValue thisValue, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            return callableEvaluator.EvaluateCallableAsync(callable, thisValue, args, context);
        }

        public IAsyncEnumerable<QsEvalContext?> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        public async ValueTask<QsEvalContext?> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            // 함수 처리
            foreach (var elem in script.Elements)
            {
                if (elem is QsFuncDeclScriptElement funcDeclElem)
                {
                    context = context.AddFunc(funcDeclElem.FuncDecl);
                }                
            }

            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var result in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                        if (!result.HasValue) return null;
                        context = result.Value;
                    }
                }
                else if (elem is QsFuncDeclScriptElement) continue;
                else if (elem is QsEnumDeclScriptElement) continue;
                else return null;
            }

            return context;
        }

        internal QsTypeInst InstantiateType(QsTypeValue testTypeValue, QsEvalContext context)
        {
            return new QsRawTypeInst(testTypeValue);
        }
    }
}