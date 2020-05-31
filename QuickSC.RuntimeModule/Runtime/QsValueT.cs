using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    class QsValue<T> : QsValue where T : struct
    {
        public T Value { get; set; }
        public QsValue(T value)
        {
            Value = value;
        }

        public override void SetValue(QsValue v)
        {
            Value = ((QsValue<T>)v).Value;
        }

        public override QsValue MakeCopy()
        {
            return new QsValue<T>(Value);
        }        
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            throw new InvalidOperationException();
        }

        public override QsTypeInst GetTypeInst()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object? obj)
        {
            return obj is QsValue<T> value &&
                   EqualityComparer<T>.Default.Equals(Value, value.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsValue<T>? left, QsValue<T>? right)
        {
            return EqualityComparer<QsValue<T>?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsValue<T>? left, QsValue<T>? right)
        {
            return !(left == right);
        }
    }

}
