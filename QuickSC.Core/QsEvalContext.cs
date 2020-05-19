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
        public ImmutableDictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> StoragesByExp { get; }
        public ImmutableDictionary<QsMemberExp, QsStaticStorage> StaticStoragesByMemberExp { get; } // (Namespace.C).x // staticStorage
        public ImmutableDictionary<QsCaptureInfoLocation, QsCaptureInfo> CaptureInfosByLocation { get; }

        // 모든 모듈의 전역 변수
        public Dictionary<(IQsMetadata?, string), QsValue> GlobalVars { get; } // TODO: IQsMetadata말고 다른 Id가 있어야 한다
        public ImmutableDictionary<string, QsValue> LocalVars { get; private set; }

        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }
        public bool bGlobalScope { get; set; }
        public ImmutableDictionary<QsCallExp, QsFuncId> FuncIdsByCallExp { get; }
        public ImmutableDictionary<QsMemberCallExp, QsFuncId> FuncIdsByMemberCallExp { get; }

        public QsEvalContext(
            ImmutableDictionary<QsExp, QsTypeValue> typeValuesByExp,
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> storagesByExp,
            ImmutableDictionary<QsCaptureInfoLocation, QsCaptureInfo> captureInfosByLocation,            
            ImmutableDictionary<string, QsValue> localVars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue? thisValue,
            bool bGlobalScope)
        {
            TypeValuesByExp = typeValuesByExp;
            TypeValuesByTypeExp = typeValuesByTypeExp;
            StoragesByExp = storagesByExp;
            CaptureInfosByLocation = captureInfosByLocation;
            
            GlobalVars = new Dictionary<(IQsMetadata?, string), QsValue>();
            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
            this.bGlobalScope = bGlobalScope;
        }

        public QsEvalContext SetLocalVars(ImmutableDictionary<string, QsValue> newLocalVars)
        {
            LocalVars = newLocalVars;
            return this;
        }

        public ImmutableDictionary<string, QsValue> GetLocalVars()
        {
            return LocalVars;
        }

        public QsValue GetLocalVar(string varName)
        {
            return LocalVars[varName];
        }
        
        public void SetLocalVar(string varName, QsValue value)
        {
            LocalVars = LocalVars.SetItem(varName, value);
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