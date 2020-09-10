using Gum.Syntax;
using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace QuickSC
{   
    public class QsEvalContext
    {
        public IQsRuntimeModule RuntimeModule { get; }
        public QsDomainService DomainService { get; }
        public QsTypeValueService TypeValueService { get; }

        private QsValue?[] privateGlobalVars;
        private QsValue?[] localVars;

        private QsEvalFlowControl flowControl;
        private ImmutableArray<Task> tasks;
        private QsValue? thisValue;
        private QsValue retValue;

        private ImmutableDictionary<ISyntaxNode, QsSyntaxNodeInfo> infosByNode;

        public QsEvalContext(
            IQsRuntimeModule runtimeModule, 
            QsDomainService domainService, 
            QsTypeValueService typeValueService,
            int privateGlobalVarCount,
            ImmutableDictionary<ISyntaxNode, QsSyntaxNodeInfo> infosByNode)
        {
            RuntimeModule = runtimeModule;
            DomainService = domainService;
            TypeValueService = typeValueService;
            
            this.infosByNode = infosByNode;
            
            localVars = new QsValue?[0];
            privateGlobalVars = new QsValue?[privateGlobalVarCount];
            flowControl = QsEvalFlowControl.None;
            tasks = ImmutableArray<Task>.Empty; ;
            thisValue = null;
            retValue = QsVoidValue.Instance;
        }

        public QsEvalContext(
            QsEvalContext other,
            QsValue?[] localVars,
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue? thisValue,
            QsValue retValue)
        {
            RuntimeModule = other.RuntimeModule;
            DomainService = other.DomainService;
            TypeValueService = other.TypeValueService;
            this.infosByNode = other.infosByNode;
            privateGlobalVars = other.privateGlobalVars;

            this.localVars = localVars;
            this.flowControl = flowControl;
            this.tasks = tasks;
            this.thisValue = thisValue;
            this.retValue = retValue;
        }

        public QsEvalContext SetTasks(ImmutableArray<Task> newTasks)
        {
            tasks = newTasks;
            return this;
        }

        public ImmutableArray<Task> GetTasks()
        {
            return tasks;
        }

        public void AddTask(Task task)
        {
            tasks = tasks.Add(task);
        }

        public async ValueTask ExecInNewFuncFrameAsync(
            QsValue?[] newLocalVars, 
            QsEvalFlowControl newFlowControl, 
            ImmutableArray<Task> newTasks, 
            QsValue? newThisValue, 
            QsValue newRetValue,
            Func<ValueTask> ActionAsync)
        {
            var prevValue = (localVars, flowControl, tasks, thisValue, retValue);
            (localVars, flowControl, tasks, thisValue, retValue) = (newLocalVars, newFlowControl, newTasks, newThisValue, newRetValue);

            try
            {
                await ActionAsync();
            }
            finally
            {
                (localVars, flowControl, tasks, thisValue, retValue) = prevValue;
            }
        }

        public TSyntaxNodeInfo GetNodeInfo<TSyntaxNodeInfo>(ISyntaxNode node) where TSyntaxNodeInfo : QsSyntaxNodeInfo
        {
            return (TSyntaxNodeInfo)infosByNode[node];
        }

        public QsValue GetStaticValue(QsVarValue varValue)
        {
            throw new NotImplementedException();
        }

        public QsValue GetPrivateGlobalVar(int index)
        {
            return privateGlobalVars[index]!;
        }

        public void InitPrivateGlobalVar(int index, QsValue value)
        {
            privateGlobalVars[index] = value;
        }

        public QsValue GetLocalVar(int index)
        {
            return localVars[index]!;
        }

        public void InitLocalVar(int i, QsValue value)
        {
            // for문 내부에서 decl할 경우 재사용하기 때문에 assert를 넣으면 안된다
            // Debug.Assert(context.LocalVars[storage.LocalIndex] == null);
            localVars[i] = value;
        }

        public bool IsFlowControl(QsEvalFlowControl testValue)
        {
            return flowControl == testValue;
        }

        public QsEvalFlowControl GetFlowControl()
        {
            return flowControl;
        }

        public void SetFlowControl(QsEvalFlowControl newFlowControl)
        {
            flowControl = newFlowControl;
        }

        public QsValue GetRetValue()
        {
            return retValue!;
        }

        public QsValue? GetThisValue()
        {
            return thisValue;
        }

        public async IAsyncEnumerable<QsValue> ExecInNewTasks(Func<IAsyncEnumerable<QsValue>> enumerable)
        {
            var prevTasks = tasks;
            tasks = ImmutableArray<Task>.Empty;

            await foreach (var v in enumerable())
                yield return v;
            
            tasks = prevTasks;
        }
    }
}