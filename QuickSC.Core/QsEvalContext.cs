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
        public QsTypeValueService TypeValueService { get; }
        public QsStaticValueService StaticValueService { get; }
        public QsAnalyzeInfo AnalyzeInfo { get; }

        public QsValue?[] PrivateGlobalVars { get; private set; }
        public QsValue?[] LocalVars { get; private set; }
        
        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }
        public QsValue RetValue { get; private set; }        

        public QsEvalContext(
            IQsRuntimeModule runtimeModule, 
            QsDomainService domainService, 
            QsTypeValueService typeValueService,
            QsStaticValueService staticValueService,
            QsAnalyzeInfo analyzeInfo)
        {
            RuntimeModule = runtimeModule;
            DomainService = domainService;
            TypeValueService = typeValueService;
            StaticValueService = staticValueService;

            AnalyzeInfo = analyzeInfo;
            LocalVars = new QsValue?[0];
            PrivateGlobalVars = new QsValue?[analyzeInfo.PrivateGlobalVarCount];
            FlowControl = QsEvalFlowControl.None;
            Tasks = ImmutableArray<Task>.Empty; ;
            ThisValue = null;
            RetValue = QsVoidValue.Instance;
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
            StaticValueService = other.StaticValueService;
            AnalyzeInfo = other.AnalyzeInfo;
            PrivateGlobalVars = other.PrivateGlobalVars;

            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
            RetValue = retValue;
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

        public (QsValue?[] prevLocalVars, QsEvalFlowControl prevFlowControl, ImmutableArray<Task> prevTasks, QsValue? prevThisValue, QsValue prevRetValue) 
            Update(QsValue?[] localVars, QsEvalFlowControl flowControl, ImmutableArray<Task> tasks, QsValue? thisValue, QsValue retValue)
        {
            var prevValue = (LocalVars, FlowControl, Tasks, ThisValue, RetValue);

            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
            RetValue = retValue;

            return prevValue;
        }

        public TSyntaxNodeInfo GetNodeInfo<TSyntaxNodeInfo>(IQsSyntaxNode node) where TSyntaxNodeInfo : QsSyntaxNodeInfo
        {
            return AnalyzeInfo.GetNodeInfo<TSyntaxNodeInfo>(node);
        }
    }
}