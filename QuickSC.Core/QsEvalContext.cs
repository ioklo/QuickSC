using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{   
    public class QsEvalContext
    {
        public QsAnalyzeInfo AnalyzeInfo { get; }

        // 모든 모듈의 전역 변수
        public Dictionary<QsVarId, QsValue> GlobalVars { get; }
        public QsValue?[] LocalVars { get; }

        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }

        public QsEvalContext(QsAnalyzeInfo analyzeInfo)
        {
            this.AnalyzeInfo = analyzeInfo;
            this.GlobalVars = new Dictionary<QsVarId, QsValue>();
            this.LocalVars = new QsValue?[0];
            this.FlowControl = QsNoneEvalFlowControl.Instance;
            this.Tasks = ImmutableArray<Task>.Empty; ;
            this.ThisValue = null;
        }

        public QsEvalContext(
            QsEvalContext other,
            QsValue?[] localVars,
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue? thisValue)
        {
            this.AnalyzeInfo = other.AnalyzeInfo;
            this.GlobalVars = other.GlobalVars;
            this.LocalVars = localVars;
            this.FlowControl = flowControl;
            this.Tasks = tasks;
            this.ThisValue = thisValue;
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
    }
}