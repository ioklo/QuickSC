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

    public class QsEvalStaticContext
    {
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValues { get; }
        public ImmutableDictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> CaptureInfosByLocation { get; }

        public QsEvalStaticContext(
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValues,
            ImmutableDictionary<QsCaptureInfoLocation, ImmutableDictionary<string, QsCaptureContextCaptureKind>> captureInfosByLocation)
        {
            TypeValues = typeValues;
            CaptureInfosByLocation = captureInfosByLocation;
        }
    }

    // TODO: EvalContext는 백트래킹을 할 것이 아니기 때문에 mutable로 바꿀 수 있을것 같다
    public struct QsEvalContext
    {
        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다        
        public QsEvalStaticContext StaticContext { get; }
        public ImmutableDictionary<string, QsFuncDecl> Funcs { get; }
        public ImmutableDictionary<string, QsValue> GlobalVars { get; }
        public ImmutableDictionary<string, QsValue> Vars { get; }

        public QsEvalFlowControl FlowControl { get; }
        public ImmutableArray<Task> Tasks { get; }
        public QsValue ThisValue { get; }
        public bool bGlobalScope { get; }
        

        public static QsEvalContext Make(QsEvalStaticContext StaticContext)
        {
            return new QsEvalContext(
                StaticContext,
                ImmutableDictionary<string, QsFuncDecl>.Empty,
                ImmutableDictionary<string, QsValue>.Empty, 
                ImmutableDictionary<string, QsValue>.Empty, 
                QsNoneEvalFlowControl.Instance,
                ImmutableArray<Task>.Empty,
                QsNullValue.Instance,
                true);
        }

        private QsEvalContext(
            QsEvalStaticContext StaticContext,
            ImmutableDictionary<string, QsFuncDecl> funcs,
            ImmutableDictionary<string, QsValue> globalVars, 
            ImmutableDictionary<string, QsValue> vars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue thisValue,
            bool bGlobalScope)
        {
            this.StaticContext = StaticContext;
            this.Funcs = funcs;
            this.GlobalVars = globalVars;
            this.Vars = vars;
            this.FlowControl = flowControl;
            this.Tasks = tasks;
            this.ThisValue = thisValue;
            this.bGlobalScope = bGlobalScope;
        }

        public QsEvalContext SetVars(ImmutableDictionary<string, QsValue> newVars)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, newVars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext SetFlowControl(QsEvalFlowControl newFlowControl)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars, newFlowControl, Tasks, ThisValue, bGlobalScope);
        }
        
        public QsEvalContext SetTasks(ImmutableArray<Task> newTasks)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars, FlowControl, newTasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext SetThisValue(QsValue newThisValue)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars, FlowControl, Tasks, newThisValue, bGlobalScope);
        }

        public QsEvalContext SetGlobalValue(string varName, QsValue value)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars.SetItem(varName, value), Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars.SetItem(varName, value), FlowControl, Tasks, ThisValue, bGlobalScope);
        }
        
        public QsEvalContext AddFunc(QsFuncDecl funcDecl)
        {
            return new QsEvalContext(StaticContext, Funcs.SetItem(funcDecl.Name, funcDecl), GlobalVars, Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext AddGlobalVar(string name, QsValue value)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars.Add(name, value), Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext AddTask(Task task)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars, FlowControl, Tasks.Add(task), ThisValue, bGlobalScope);
        }

        public QsEvalContext SetGlobalScope(bool bGlobalScope)
        {
            return new QsEvalContext(StaticContext, Funcs, GlobalVars, Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsTypeValue? GetTypeValue(QsTypeExp typeExp)
        {
            if (StaticContext.TypeValues.TryGetValue(typeExp, out var typeValue))
                return typeValue;

            return null;
        }

        public QsValue? GetValue(string varName)
        {
            if (Vars.TryGetValue(varName, out var retValue))
                return retValue;

            return null;
        }

        public QsValue? GetGlobalValue(string varName)
        {
            if (GlobalVars.TryGetValue(varName, out var retValue))
                return retValue;

            return null;
        }

        public bool HasVar(string varName)
        {
            return Vars.ContainsKey(varName) || GlobalVars.ContainsKey(varName);
        }

        public QsFuncDecl? GetFunc(string funcName)
        {
            return Funcs.GetValueOrDefault(funcName);
        }
    }
}