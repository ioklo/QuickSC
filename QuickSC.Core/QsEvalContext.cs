﻿using QuickSC.Syntax;
using System;
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

    public struct QsEvalContext
    {
        // TODO: QsFuncDecl을 직접 사용하지 않고, QsModule에서 정의한 Func을 사용해야 한다        
        public ImmutableDictionary<string, QsFuncDecl> Funcs { get; }
        public ImmutableDictionary<string, QsValue> Vars { get; }
        public QsEvalFlowControl FlowControl { get; }
        public ImmutableArray<Task> Tasks { get; }
        public QsValue ThisValue { get; }

        public static QsEvalContext Make()
        {
            return new QsEvalContext(
                ImmutableDictionary<string, QsFuncDecl>.Empty, 
                ImmutableDictionary<string, QsValue>.Empty, 
                QsNoneEvalFlowControl.Instance,
                ImmutableArray<Task>.Empty,
                QsNullValue.Instance);
        }

        private QsEvalContext(
            ImmutableDictionary<string, QsFuncDecl> funcs, 
            ImmutableDictionary<string, QsValue> vars, 
            QsEvalFlowControl flowControl,
            ImmutableArray<Task> tasks,
            QsValue thisValue)
        {
            this.Funcs = funcs;
            this.Vars = vars;
            this.FlowControl = flowControl;
            this.Tasks = tasks;
            this.ThisValue = thisValue;
        }

        public QsEvalContext SetVars(ImmutableDictionary<string, QsValue> newVars)
        {
            return new QsEvalContext(Funcs, newVars, FlowControl, Tasks, ThisValue);
        }

        public QsEvalContext SetFlowControl(QsEvalFlowControl newFlowControl)
        {
            return new QsEvalContext(Funcs, Vars, newFlowControl, Tasks, ThisValue);
        }

        public QsEvalContext SetTasks(ImmutableArray<Task> newTasks)
        {
            return new QsEvalContext(Funcs, Vars, FlowControl, newTasks, ThisValue);
        }

        public QsEvalContext SetThisValue(QsValue newThisValue)
        {
            return new QsEvalContext(Funcs, Vars, FlowControl, Tasks, newThisValue);
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            return new QsEvalContext(Funcs, Vars.SetItem(varName, value), FlowControl, Tasks, ThisValue);
        }

        public QsEvalContext AddFunc(QsFuncDecl funcDecl)
        {
            return new QsEvalContext(Funcs.SetItem(funcDecl.Name, funcDecl), Vars, FlowControl, Tasks, ThisValue);
        }

        public QsEvalContext AddTask(Task task)
        {
            return new QsEvalContext(Funcs, Vars, FlowControl, Tasks.Add(task), ThisValue);
        }

        public QsValue? GetValue(string varName)
        {
            return Vars.GetValueOrDefault(varName);
        }

        public bool HasVar(string varName)
        {
            return Vars.ContainsKey(varName);
        }

        public QsFuncDecl? GetFunc(string funcName)
        {
            return Funcs.GetValueOrDefault(funcName);
        }
    }
}