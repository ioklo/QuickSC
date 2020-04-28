using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace QuickSC
{
    // placeholder
    public abstract class QsValue
    {
        public abstract bool SetValue(QsValue v);
        public abstract QsValue MakeCopy();
    }

    public class QsValue<T> : QsValue
    {
        public T Value { get; set; }
        public QsValue(T value)
        {
            Value = value;
        }

        public override bool SetValue(QsValue v)
        {
            if (v is QsValue<T> tv)
            {
                Value = tv.Value;
                return true;
            }

            return false;
        }

        public override QsValue MakeCopy()
        {
            return new QsValue<T>(Value);
        }
    }

    public class QsNullValue : QsValue
    {
        public static QsNullValue Instance { get; } = new QsNullValue();
        private QsNullValue() { }

        public override bool SetValue(QsValue v)
        {
            return v is QsNullValue;
        }

        public override QsValue MakeCopy()
        {
            return Instance;
        }
    }

    // Internal Structure
    // qs type : c# type
    // null   : QsNullValue
    // int    : QsValue<int> 
    // bool   : QsValue<bool>
    // int &  : QsRefValue, or QsValue<QsValue<int>>
    // string : QsValue<QsString> or QsValue<string> // 이미 string이 c#에서 reftype이기 때문에 int, bool이랑 동작을 구분하지 않아도 된다
    // class T -> { type: typeInfo, ... } : QsValue<QsRecord> // 
    // func -> { captures..., Invoke: func }

    //public class QsRecord
    //{
    //    public Dictionary<string, QsValue> Fields { get; }

    //    public QsRecord()
    //    {
    //        Fields = new Dictionary<string, QsValue>();
    //    }
    //}

    public abstract class QsCallable
    {
    }

    public class QsFuncCallable : QsCallable
    {
        // TODO: Syntax직접 쓰지 않고, QsModule에서 정의한 것들을 써야 한다
        public QsFuncDecl FuncDecl { get; }
        public QsFuncCallable(QsFuncDecl funcDecl)
        {
            FuncDecl = funcDecl;
        }
    }

    public class QsLambdaCallable : QsCallable
    {
        // capture는 새로운 QsValue를 만들거나(value), 이전 QsValue를 그대로 가져와서 (ref-capture)
        public ImmutableDictionary<string, QsValue> Captures { get; }

        // TODO: Syntax직접 쓰지 않고, QsModule에서 정의한 것들을 써야 한다
        public QsLambdaExp Exp { get; }

        public QsLambdaCallable(QsLambdaExp exp, ImmutableDictionary<string, QsValue> captures)
        {
            Exp = exp;
            Captures = captures;
        }
    }
}

