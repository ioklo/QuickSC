namespace QuickSC
{
    // 일단 Value는 String만 있다고 하자.. 추후에 조금씩 추가하는 걸로 
    public abstract class QsValue
    {

    }

    public class QsNullValue : QsValue
    {
        public static QsNullValue Value = new QsNullValue();
        private QsNullValue() { }
    }

    public class QsStringValue : QsValue
    {
        public string Value { get; }
        public QsStringValue(string value) { Value = value; }
    }
}