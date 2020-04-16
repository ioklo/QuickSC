﻿namespace QuickSC
{
    // 일단 Value는 String만 있다고 하자.. 추후에 조금씩 추가하는 걸로 
    public abstract class QsValue
    {

    }

    public class QsNullValue : QsValue
    {
        public static QsNullValue Instance { get; } = new QsNullValue();
        private QsNullValue() { }
    }

    public class QsBoolValue : QsValue
    {
        public bool Value { get; }
        public QsBoolValue(bool value) { Value = value; }
    }

    public class QsIntValue : QsValue
    {
        public int Value { get; }
        public QsIntValue(int value) { Value = value; }
    }

    public class QsStringValue : QsValue
    {
        public string Value { get; }
        public QsStringValue(string value) { Value = value; }
    }
}