using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;

namespace QuickSC
{
    // 레퍼런스용 Big Step Evaluator, 
    // TODO: Small Step으로 가야하지 않을까 싶다 (yield로 실행 point 잡는거 해보면 재미있을 것 같다)
    public class QsEvaluator
    {
        QsExpEvaluator expEvaluator;
        QsStmtEvaluator stmtEvaluator;
        IQsRuntimeModule runtimeModule;

        public QsEvaluator(IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {            
            this.expEvaluator = new QsExpEvaluator(this, runtimeModule);
            this.stmtEvaluator = new QsStmtEvaluator(this, expEvaluator, commandProvider, runtimeModule);
            this.runtimeModule = runtimeModule;
        }
        
        public QsFuncInst GetFuncInst(QsFuncValue funcValue, QsEvalContext context)
        {
            // 1. context에 캐시되어있는지 본다
            // if (context.FuncInstsById.TryGetValue((funcId, typeArgs), out var cachedFuncInst))
            //     return cachedFuncInst;
            if (funcValue.FuncId.Metadata != null)
            {
                return ((IQsModule)funcValue.FuncId.Metadata).GetFuncInst(funcValue);
            }
            else
            {
                // 1. Script의 T 치환, or typeenv를 그냥 박기
                return ;
            }

            throw new NotImplementedException();
        }
        
        public QsFuncInst GetFuncInst(QsValue thisValue, QsFuncValue funcValue, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public QsTypeInst GetTypeInst(QsTypeValue typeValue, QsEvalContext context)
        {
            // context.TypeValuesByTypeExp[typeExp]
            // return new QsRawTypeInst(testTypeValue);

            throw new NotImplementedException();
        }

        public ImmutableDictionary<string, QsValue?> MakeCaptures(QsCaptureInfo captureInfo, QsEvalContext context)
        {
            var captures = ImmutableDictionary.CreateBuilder<string, QsValue?>();
            foreach (var (name, kind) in captureInfo.Captures)
            {
                var origValue = context.GetLocalVar(name);

                QsValue value;
                if (kind == QsCaptureContextCaptureKind.Copy)
                {
                    value = origValue!.MakeCopy();
                }
                else
                {
                    Debug.Assert(kind == QsCaptureContextCaptureKind.Ref);
                    value = origValue!;
                }

                captures.Add(name, value);
            }

            return captures.ToImmutable();
        }


        async IAsyncEnumerable<QsValue> EvaluateScriptFuncInstSeqAsync(
            QsScriptFuncInst scriptFuncInst,
            ImmutableArray<QsValue> args,
            QsEvalContext context)
        {
            Debug.Assert(scriptFuncInst.Params.Length == args.Length);
            for (int i = 0; i < args.Length; i++)
                context.SetLocalVar(scriptFuncInst.Params[i], args[i]);

            await foreach (var value in EvaluateStmtAsync(scriptFuncInst.Body, context))
            {
                yield return value;
            }
        }

        public async ValueTask<QsValue> EvaluateFuncInstAsync(QsValue? thisValue, QsFuncInst funcInst, ImmutableArray<QsValue> args, QsEvalContext context)
        {
            if (funcInst is QsScriptFuncInst scriptFuncInst)
            {
                // (Capture한 곳의 this), (MemberExp의 this), Static의 경우 this
                if (scriptFuncInst.CapturedThis != null)
                    thisValue = scriptFuncInst.CapturedThis;
                else if (!scriptFuncInst.bThisCall)
                    thisValue = null;

                if (scriptFuncInst.bSeqCall)
                {
                    // context 복제
                    QsEvalContext newContext = context.MakeCopy();
                    newContext.SetLocalVars(scriptFuncInst.Captures);
                    newContext.SetTasks(ImmutableArray<Task>.Empty);
                    newContext.FlowControl = QsNoneEvalFlowControl.Instance;
                    newContext.ThisValue = thisValue;
                    newContext.bGlobalScope = false;

                    var asyncEnum = EvaluateScriptFuncInstSeqAsync(scriptFuncInst, args, newContext);
                    return runtimeModule.MakeAsyncEnumerable(asyncEnum);
                }

                var (prevLocalVars, prevTasks, prevThisValue) = (context.GetLocalVars(), context.GetTasks(), context.ThisValue);

                context.SetLocalVars(scriptFuncInst.Captures)
                        .SetTasks(ImmutableArray<Task>.Empty);
                context.ThisValue = thisValue;

                Debug.Assert(scriptFuncInst.Params.Length == args.Length);
                for (int i = 0; i < args.Length; i++)
                    context.SetLocalVar(scriptFuncInst.Params[i], args[i]);

                await foreach (var _ in EvaluateStmtAsync(scriptFuncInst.Body, context)) { }

                context.SetLocalVars(prevLocalVars)
                        .SetTasks(prevTasks);
                context.ThisValue = prevThisValue;

                if (context.FlowControl is QsReturnEvalFlowControl returnFlowControl)
                {
                    context.FlowControl = QsNoneEvalFlowControl.Instance;
                    return returnFlowControl.Value;
                }
                else
                {
                    Debug.Assert(context.FlowControl == QsNoneEvalFlowControl.Instance);
                    return QsVoidValue.Instance;
                }
            }
            else if (funcInst is QsNativeFuncInst nativeFuncInst)
            {
                return await nativeFuncInst.CallAsync(thisValue, args);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        public IAsyncEnumerable<QsValue> EvaluateStmtAsync(QsStmt stmt, QsEvalContext context)
        {
            return stmtEvaluator.EvaluateStmtAsync(stmt, context);
        }
        
        public async ValueTask<QsEvalContext?> EvaluateScriptAsync(QsScript script, QsEvalContext context)
        {
            foreach(var elem in script.Elements)
            {
                if (elem is QsStmtScriptElement statementElem)
                {
                    await foreach (var value in stmtEvaluator.EvaluateStmtAsync(statementElem.Stmt, context))
                    {
                    }
                }
            }

            return context;
        }
    }
}