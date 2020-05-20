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
        // 실행을 위한 기본 정보
        public ImmutableDictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> StoragesByExp { get; }
        // public ImmutableDictionary<QsMemberExp, QsStaticStorage> StaticStoragesByMemberExp { get; } // (Namespace.C).x // staticStorage
        public ImmutableDictionary<QsCaptureInfoLocation, QsCaptureInfo> CaptureInfosByLocation { get; }
        public ImmutableDictionary<QsExp, QsFuncValue> FuncValuesByExp { get; }
        public ImmutableDictionary<QsForeachStmt, QsForeachInfo> ForeachInfosByForEachStmt { get; internal set; }

        // 모든 모듈의 전역 변수
        public Dictionary<(IQsMetadata?, string), QsValue?> GlobalVars { get; } // TODO: IQsMetadata말고 다른 Id가 있어야 한다
        public ImmutableDictionary<string, QsValue?> LocalVars { get; private set; }

        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }
        public bool bGlobalScope { get; set; }        

        public QsEvalContext(
            ImmutableDictionary<QsExp, QsTypeValue> typeValuesByExp,
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            ImmutableDictionary<QsExp, (QsStorage Storage, QsStorageKind Kind)> storagesByExp,
            ImmutableDictionary<QsCaptureInfoLocation, QsCaptureInfo> captureInfosByLocation,            
            ImmutableDictionary<string, QsValue?> localVars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue? thisValue,
            bool bGlobalScope)
        {
            TypeValuesByExp = typeValuesByExp;
            TypeValuesByTypeExp = typeValuesByTypeExp;
            StoragesByExp = storagesByExp;
            CaptureInfosByLocation = captureInfosByLocation;
            
            GlobalVars = new Dictionary<(IQsMetadata?, string), QsValue?>();
            LocalVars = localVars;
            FlowControl = flowControl;
            Tasks = tasks;
            ThisValue = thisValue;
            this.bGlobalScope = bGlobalScope;
        }

        public QsEvalContext SetLocalVars(ImmutableDictionary<string, QsValue?> newLocalVars)
        {
            LocalVars = newLocalVars;
            return this;
        }

        public ImmutableDictionary<string, QsValue?> GetLocalVars()
        {
            return LocalVars;
        }

        public QsValue? GetLocalVar(string varName)
        {
            return LocalVars[varName];
        }
        
        public void SetLocalVar(string varName, QsValue? value)
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

        public static QsEvalContext Make(QsAnalyzerContext analyzerContext)
        {
            new QsEvalContext(
                analyzerContext.TypeValuesByExp.ToImmutableDictionary(),
                analyzerContext.TypeValuesByTypeExp.ToImmutableDictionary(),
                analyzerContext.StoragesByExp.ToImmutableDictionary(),
                analyzerContext.CaptureInfosByLocation.ToImmutableDictionary(),
                ImmutableDictionary<string, QsValue?>.Empty,
                QsNoneEvalFlowControl.Instance,
                ImmutableArray<Task>.Empty,
                null,
                true);

        }

        public void AddTask(Task task)
        {
            Tasks = Tasks.Add(task);
        }

        public QsEvalContext MakeCopy()
        {
            return new QsEvalContext(TypeValuesByExp, TypeValuesByTypeExp, StoragesByExp, CaptureInfosByLocation, LocalVars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }
    }
}