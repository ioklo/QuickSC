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
        // 실행을 위한 기본 정보,         
        public ImmutableDictionary<QsExp, QsTypeValue> TypeValuesByExp { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        // 위의 것들은 제거
        public ImmutableDictionary<QsExp, QsEvalExp> EvalExpsByExp { get; }
        public ImmutableDictionary<QsExp, QsFuncValue> FuncValuesByExp { get; }
        public ImmutableDictionary<QsForeachStmt, QsForeachInfo> ForeachInfosByForEachStmt { get; internal set; }
        public ImmutableDictionary<QsVarDecl, QsEvalVarDecl> EvalVarDeclsByVarDecl { get; }
        public ImmutableDictionary<QsExp, QsEvalInfo> EvalInfosByExp { get; }

        // 모든 모듈의 전역 변수
        public Dictionary<QsVarId, QsValue> GlobalVars { get; }
        public QsValue?[] LocalVars { get; }

        public QsEvalFlowControl FlowControl { get; set; }
        public ImmutableArray<Task> Tasks { get; private set; }
        public QsValue? ThisValue { get; set; }

        public QsEvalContext(
            ImmutableDictionary<QsExp, QsTypeValue> typeValuesByExp,
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            ImmutableDictionary<QsExp, QsEvalExp> evalExpsByExp,
            ImmutableDictionary<QsExp, QsFuncValue> funcValuesByExp,
            ImmutableDictionary<QsForeachStmt, QsForeachInfo> foreachInfosByForEachStmt,
            ImmutableDictionary<QsVarDecl, QsEvalVarDecl> evalVarDeclsByVarDecl,
            ImmutableDictionary<QsExp, QsEvalInfo> evalInfosByExp)
        {
            this.TypeValuesByExp = typeValuesByExp;
            this.TypeValuesByTypeExp = typeValuesByTypeExp;
            this.EvalExpsByExp = evalExpsByExp;
            this.FuncValuesByExp = funcValuesByExp;
            this.ForeachInfosByForEachStmt = foreachInfosByForEachStmt;
            this.EvalVarDeclsByVarDecl = evalVarDeclsByVarDecl;
            this.EvalInfosByExp = evalInfosByExp;
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
            this.TypeValuesByExp = other.TypeValuesByExp;
            this.TypeValuesByTypeExp = other.TypeValuesByTypeExp;
            this.EvalExpsByExp = other.EvalExpsByExp;
            this.FuncValuesByExp = other.FuncValuesByExp;
            this.ForeachInfosByForEachStmt = other.ForeachInfosByForEachStmt;
            this.EvalVarDeclsByVarDecl = other.EvalVarDeclsByVarDecl;
            this.EvalInfosByExp = other.EvalInfosByExp;
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