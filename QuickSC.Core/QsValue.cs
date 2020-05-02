using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace QuickSC
{
    // runtime placeholder
    public abstract class QsValue
    {
        public abstract bool SetValue(QsValue v);
        public abstract QsValue MakeCopy();

        // 뭘 리턴해야 하는거냐
        public abstract QsCallable? GetMemberFuncs(QsMemberFuncId funcId);
        public abstract QsValue? GetMemberValue(string varName);
    }

    public class QsValue<T> : QsValue where T : struct
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

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            throw new NotImplementedException();
        }

        public override QsValue GetMemberValue(string varName)
        {
            throw new NotImplementedException();
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

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            throw new InvalidOperationException();
        }

        public override QsValue GetMemberValue(string varName)
        {
            throw new InvalidOperationException();
        }
    }
    
    public abstract class QsObject
    {
        public virtual QsCallable? GetMemberFuncs(QsMemberFuncId funcId) { return null; }
        public virtual QsValue? GetMemberValue(string varName) { return null; }

        protected static TObject? GetObject<TObject>(QsValue value) where TObject : QsObject
        {
            if (value is QsObjectValue objValue && objValue.Object is TObject obj)
                return obj;

            return null;
        }
        
    }    
   
    public class QsObjectValue : QsValue
    {
        public QsObject Object { get; private set; }

        public QsObjectValue(QsObject obj)
        {
            Object = obj;
        }

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            return Object.GetMemberFuncs(funcId);
        }

        public override QsValue? GetMemberValue(string varName)
        {
            return Object.GetMemberValue(varName);
        }

        public override QsValue MakeCopy()
        {
            return new QsObjectValue(Object);
        }

        public override bool SetValue(QsValue value)
        {
            if (value is QsObjectValue objValue)
            {
                Object = objValue.Object;
                return true;
            }

            return false;
        }
    }

    // Internal Structure
    // qs type : c# type
    // null   : QsNullValue
    // int    : QsValue<int> 
    // bool   : QsValue<bool>
    // int &  : QsRefValue(QsValue<int>)
    // X &    : QsRefValue(QsValue) 
    // string : QsObjectValue(QsStringObject) 
    // class T -> { type: typeInfo, ... } : QsObjectValue(QsClassObject) // 
    // { captures..., Invoke: func } 
    // () => { }
    
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

    public class QsNativeCallable : QsCallable
    {
        public Func<QsValue, ImmutableArray<QsValue>, QsEvalContext, ValueTask<QsEvalResult<QsValue>>> Invoker { get; }
        public QsNativeCallable(Func<QsValue, ImmutableArray<QsValue>, QsEvalContext, ValueTask<QsEvalResult<QsValue>>> invoker)
        {
            Invoker = invoker;
        }
    }
}

