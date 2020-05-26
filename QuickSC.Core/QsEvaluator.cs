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
        QsDomainService domainService;

        QsExpEvaluator expEvaluator;
        QsStmtEvaluator stmtEvaluator;
        IQsRuntimeModule runtimeModule;

        public QsEvaluator(QsDomainService domainService, IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {
            this.domainService = domainService;

            this.expEvaluator = new QsExpEvaluator(this, runtimeModule, domainService);
            this.stmtEvaluator = new QsStmtEvaluator(this, expEvaluator, commandProvider, runtimeModule);
            this.runtimeModule = runtimeModule;
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

        
        public QsValue EvaluateStorage(QsStorage storage, QsEvalContext context)
        {
            // 전역 변수는 
            switch (storage)
            {
                // Instance는 this도 캡쳐해야 한다
                //case QsFuncStorage funcStorage:
                //    return new QsFuncInstValue(domainService.GetFuncInst(funcStorage.FuncId,);
                
                case QsLocalVarStorage localStorage:
                    return context.LocalVars[localStorage.LocalIndex]!;

                case QsGlobalVarStorage globalStorage:
                    return context.GlobalVars[globalStorage.VarId]!;

                case QsStaticVarStorage staticStorage:
                    throw new NotImplementedException();
                //    return context.GetStaticValue(staticStorage.TypeValue).GetMemberValue(staticStorage.VarId);

                case QsInstanceVarStorage instStorage:
                    return context.ThisValue!.GetMemberValue(instStorage.VarId);

                default:
                    throw new NotImplementedException();
            }
        }

        internal QsValue GetDefaultValue(QsTypeValue typeValue, QsEvalContext context)
        {
            throw new NotImplementedException();
        }

        public ImmutableDictionary<string, QsValue?> MakeCaptures(QsCaptureInfo captureInfo, QsEvalContext context)
        {
            var captures = ImmutableDictionary.CreateBuilder<string, QsValue?>();
            foreach (var capture in captureInfo.Captures)
            {
                var origValue = EvaluateStorage(capture.Storage, context);
                
                QsValue value;
                if (capture.Kind == QsCaptureKind.Copy)
                {
                    value = origValue!.MakeCopy();
                }
                else
                {
                    Debug.Assert(capture.Kind == QsCaptureKind.Ref);
                    value = origValue!;
                }

                captures.Add(capture.Name, value);
            }

            return captures.ToImmutable();
        }

        async IAsyncEnumerable<QsValue> EvaluateScriptFuncInstSeqAsync(
            QsScriptFuncInst scriptFuncInst,
            ImmutableArray<QsValue> args,
            QsEvalContext context)
        {
            for (int i = 0; i < args.Length; i++)
                context.LocalVars[i] = args[i];

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
                    var newContext = new QsEvalContext(
                        context,
                        scriptFuncInst.Captures,
                        ImmutableArray<Task>.Empty,
                        QsNoneEvalFlowControl.Instance,
                        thisValue);

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
            // 모듈은 Script를 묶은 것이므로 여기가 아니다
            // 일단 여기서             
            InitGlobalVariables(context);

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