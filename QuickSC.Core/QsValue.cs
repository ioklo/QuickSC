using System;
using System.Collections.Generic;

namespace QuickSC
{
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
        public bool Value { get; set; }
        public QsBoolValue(bool value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsBoolValue value &&
                   Value == value.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsBoolValue? left, QsBoolValue? right)
        {
            return EqualityComparer<QsBoolValue>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBoolValue? left, QsBoolValue? right)
        {
            return !(left == right);
        }
    }

    public class QsIntValue : QsValue
    {
        public int Value { get; set; }
        public QsIntValue(int value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIntValue value &&
                   Value == value.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIntValue? left, QsIntValue? right)
        {
            return EqualityComparer<QsIntValue>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIntValue? left, QsIntValue? right)
        {
            return !(left == right);
        }
    }
    
    public class QsStringValue : QsValue
    {
        public string Value { get; set; }
        public QsStringValue(string value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsStringValue value &&
                   Value == value.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsStringValue? left, QsStringValue? right)
        {
            return EqualityComparer<QsStringValue>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStringValue? left, QsStringValue? right)
        {
            return !(left == right);
        }
    }
}