using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
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
        public QsStaticValueService StaticValueService { get; }
        public QsAnalyzeInfo AnalyzeInfo { get; }

        // 모든 모듈의 전역 변수
        public Dictionary<QsMetaItemId, QsValue> GlobalVars { get; }

        public QsValue?[] LocalVars { get; private set; }
        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }

        public QsEvalContext(
            IQsRuntimeModule runtimeModule, 
            QsDomainService domainService, 
            QsStaticValueService staticValueService,
            QsAnalyzeInfo analyzeInfo)
        {
            RuntimeModule = runtimeModule;
            DomainService = domainService;
            StaticValueService = staticValueService;

            AnalyzeInfo = analyzeInfo;
            GlobalVars = new Dictionary<QsMetaItemId, QsValue>();

            LocalVars = new QsValue?[0];
            FlowControl = QsNoneEvalFlowControl.Instance;
            Tasks = ImmutableArray<Task>.Empty; ;
            ThisValue = null;            
        }

        public QsEvalContext(
            QsEvalContext other,
            QsValue?[] localVars,
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue? thisValue)
        {
            RuntimeModule = other.RuntimeModule;
            DomainService = other.DomainService;
            StaticValueService = other.StaticValueService;
            AnalyzeInfo = other.AnalyzeInfo;
            GlobalVars = other.GlobalVars;

            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
        }

        public QsEvalContext SetTasks(ImmutableArray<Task> newTasks)
        {
            Tasks = newTasks;
            return this;
        }

        public ImmutableArray<Task> GetTasks()
        {
            return Tasks;
        }

        public void AddTask(Task task)
        {
            Tasks = Tasks.Add(task);
        }

        public (QsValue?[] prevLocalVars, QsEvalFlowControl prevFlowControl, ImmutableArray<Task> prevTasks, QsValue? prevThisValue) 
            Update(QsValue?[] localVars, QsEvalFlowControl flowControl, ImmutableArray<Task> tasks, QsValue? thisValue)
        {
            var prevValue = (LocalVars, FlowControl, Tasks, ThisValue);

            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;

            return prevValue;
        }
    }
}