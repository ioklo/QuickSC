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
        public QsEvalStaticContext(ImmutableDictionary<QsTypeExp, QsTypeValue> typeValues)
        {
            TypeValues = typeValues;
        }
    }

    public struct QsEvalContext
    {
        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다        
        QsEvalStaticContext staticContext;
        public ImmutableDictionary<string, QsFuncDecl> Funcs { get; }
        public ImmutableDictionary<string, QsValue> GlobalVars { get; }
        public ImmutableDictionary<string, QsValue> Vars { get; }
        public QsEvalFlowControl FlowControl { get; }
        public ImmutableArray<Task> Tasks { get; }
        public QsValue ThisValue { get; }
        public bool bGlobalScope { get; }

        public static QsEvalContext Make()
        {
            return new QsEvalContext(
                ImmutableDictionary<string, QsFuncDecl>.Empty,
                ImmutableDictionary<string, QsValue>.Empty, 
                ImmutableDictionary<string, QsValue>.Empty, 
                QsNoneEvalFlowControl.Instance,
                ImmutableArray<Task>.Empty,
                QsNullValue.Instance,
                true);
        }

        private QsEvalContext(
            ImmutableDictionary<string, QsFuncDecl> funcs,
            ImmutableDictionary<string, QsValue> globalVars, 
            ImmutableDictionary<string, QsValue> vars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue thisValue,
            bool bGlobalScope)
        {
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
            return new QsEvalContext(Funcs, GlobalVars, newVars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext SetFlowControl(QsEvalFlowControl newFlowControl)
        {
            return new QsEvalContext(Funcs, GlobalVars, Vars, newFlowControl, Tasks, ThisValue, bGlobalScope);
        }
        
        public QsEvalContext SetTasks(ImmutableArray<Task> newTasks)
        {
            return new QsEvalContext(Funcs, GlobalVars, Vars, FlowControl, newTasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext SetThisValue(QsValue newThisValue)
        {
            return new QsEvalContext(Funcs, GlobalVars, Vars, FlowControl, Tasks, newThisValue, bGlobalScope);
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            if(bGlobalScope)
            {
                return new QsEvalContext(Funcs, GlobalVars.SetItem(varName, value), Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
            }
            else
            {
                return new QsEvalContext(Funcs, GlobalVars, Vars.SetItem(varName, value), FlowControl, Tasks, ThisValue, bGlobalScope);
            }
        }
        
        public QsEvalContext AddFunc(QsFuncDecl funcDecl)
        {
            return new QsEvalContext(Funcs.SetItem(funcDecl.Name, funcDecl), GlobalVars, Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext AddGlobalVar(string name, QsValue value)
        {
            return new QsEvalContext(Funcs, GlobalVars.Add(name, value), Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsEvalContext AddTask(Task task)
        {
            return new QsEvalContext(Funcs, GlobalVars, Vars, FlowControl, Tasks.Add(task), ThisValue, bGlobalScope);
        }

        public QsEvalContext SetGlobalScope(bool bGlobalScope)
        {
            return new QsEvalContext(Funcs, GlobalVars, Vars, FlowControl, Tasks, ThisValue, bGlobalScope);
        }

        public QsTypeValue? GetTypeValue(QsTypeExp typeExp)
        {
            if (staticContext.TypeValues.TryGetValue(typeExp, out var typeValue))
                return typeValue;

            return null;
        }

        public QsValue? GetValue(string varName)
        {
            QsValue retValue;
            if (Vars.TryGetValue(varName, out retValue))
                return retValue;

            if (GlobalVars.TryGetValue(varName, out retValue))
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