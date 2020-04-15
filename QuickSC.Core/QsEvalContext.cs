using System.Collections.Immutable;

namespace QuickSC
{
    public struct QsEvalContext
    {
        ImmutableDictionary<string, QsValue> vars;

        public static QsEvalContext Make()
        {
            return new QsEvalContext(ImmutableDictionary<string, QsValue>.Empty);
        }

        private QsEvalContext(ImmutableDictionary<string, QsValue> vars)
        {
            this.vars = vars;
        }

        public QsEvalContext SetValue(string varName, QsValue value)
        {
            return new QsEvalContext(vars.SetItem(varName, value));
        }

        public QsValue? GetValue(string varName)
        {
            return vars.GetValueOrDefault(varName);
        }
    }
}