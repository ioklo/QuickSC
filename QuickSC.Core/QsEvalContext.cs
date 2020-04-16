using System.Collections.Immutable;

namespace QuickSC
{
    public enum QsEvalContextLoopControl
    {
        None,
        Break,
        Continue,
    }

    public struct QsEvalContext
    {
        public ImmutableDictionary<string, QsValue> Vars { get; }
        public QsEvalContextLoopControl LoopControl { get; }

        public static QsEvalContext Make()
        {
            return new QsEvalContext(ImmutableDictionary<string, QsValue>.Empty, QsEvalContextLoopControl.None);
        }

        private QsEvalContext(ImmutableDictionary<string, QsValue> vars, QsEvalContextLoopControl loopControl)
        {
            this.Vars = vars;
            this.LoopControl = loopControl;
        }

        public QsEvalContext SetVars(ImmutableDictionary<string, QsValue> vars)
        {
            return new QsEvalContext(vars, LoopControl);
        }

        public QsEvalContext SetLoopControl(QsEvalContextLoopControl newLoopControl)
        {
            return new QsEvalContext(Vars, newLoopControl);
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            return new QsEvalContext(Vars.SetItem(varName, value), LoopControl);
        }

        public QsValue? GetValue(string varName)
        {
            return Vars.GetValueOrDefault(varName);
        }
    }
}