namespace QuickSC
{
    public abstract class QsEvalFlowControl { }

    class QsNoneEvalFlowControl : QsEvalFlowControl
    {
        public static QsNoneEvalFlowControl Instance { get; } = new QsNoneEvalFlowControl();
        private QsNoneEvalFlowControl() { }
    }

    class QsBreakEvalFlowControl : QsEvalFlowControl
    {
        public static QsBreakEvalFlowControl Instance { get; } = new QsBreakEvalFlowControl();
        private QsBreakEvalFlowControl() { }
    }

    class QsContinueEvalFlowControl : QsEvalFlowControl
    {
        public static QsContinueEvalFlowControl Instance { get; } = new QsContinueEvalFlowControl();
        private QsContinueEvalFlowControl() { }
    }

    class QsReturnEvalFlowControl : QsEvalFlowControl
    {
        public QsValue Value { get; }
        public QsReturnEvalFlowControl(QsValue value) { Value = value; }
    }
}