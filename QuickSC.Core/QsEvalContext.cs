using QuickSC.StaticAnalyzer;
using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{   
    public abstract class QsEvalFlowControl { }

    public class QsNoneEvalFlowControl : QsEvalFlowControl
    {
        public static QsNoneEvalFlowControl Instance { get; } = new QsNoneEvalFlowControl();
        private QsNoneEvalFlowControl() { }
    }

    public class QsBreakEvalFlowControl : QsEvalFlowControl
    {
        public static QsBreakEvalFlowControl Instance { get; } = new QsBreakEvalFlowControl();
        private QsBreakEvalFlowControl() { }
    }

    public class QsContinueEvalFlowControl : QsEvalFlowControl
    {
        public static QsContinueEvalFlowControl Instance { get; } = new QsContinueEvalFlowControl();
        private QsContinueEvalFlowControl() { }
    }

    public class QsReturnEvalFlowControl : QsEvalFlowControl
    { 
        public QsValue Value { get; }
        public QsReturnEvalFlowControl(QsValue value) { Value = value; }
    }

    public class QsYieldEvalFlowControl : QsEvalFlowControl
    {
        public QsValue Value { get; }
        public QsYieldEvalFlowControl(QsValue value) { Value = value; }
    }
    
    public class QsEvalContext
    {
        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다       

        // 실행을 위한 기본 정보
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> StoragesByExp { get; }
        public ImmutableDictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> CaptureInfosByLocation { get; }

        // 모든 모듈의 전역 변수
        public Dictionary<(IQsMetadata?, string), QsValue> GlobalVars { get; } // TODO: IQsMetadata말고 다른 Id가 있어야 한다
        public ImmutableDictionary<string, QsValue> LocalVars { get; private set; }

        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue ThisValue { get; set; }
        public bool bGlobalScope { get; set; }
        
        public QsEvalContext(
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValues,
            ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> storagesByExp,
            ImmutableDictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> captureInfosByLocation,            
            ImmutableDictionary<string, QsValue> localVars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue thisValue,
            bool bGlobalScope)
        {
            TypeValuesByTypeExp = typeValues;
            StoragesByExp = storagesByExp;
            CaptureInfosByLocation = captureInfosByLocation;
            
            GlobalVars = new Dictionary<(IQsMetadata?, string), QsValue>();
            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
            this.bGlobalScope = bGlobalScope;
        }

        public void SetLocalVars(ImmutableDictionary<string, QsValue> newLocalVars)
        {
            LocalVars = newLocalVars;
        }

        public ImmutableDictionary<string, QsValue> GetLocalVars()
        {
            return LocalVars;
        }

        public QsValue GetLocalValue(string varName)
        {
            return LocalVars[varName];
        }
        
        public void SetLocalValue(string varName, QsValue value)
        {
            LocalVars = LocalVars.SetItem(varName, value);
        }

        public void SetTasks(ImmutableArray<Task> newTasks)
        {
            Tasks = newTasks;
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