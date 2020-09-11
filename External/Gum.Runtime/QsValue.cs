using Gum.CompileTime;
using QuickSC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickSC
{
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

    // 값
    public abstract class QsValue
    {
        public abstract void SetValue(QsValue fromValue);
        public abstract QsValue MakeCopy();
    }

    public class QsFuncInstValue : QsValue
    {
        public QsFuncInst? FuncInst { get; private set; }

        public QsFuncInstValue()
        {
            FuncInst = null;
        }

        public QsFuncInstValue(QsFuncInst? funcInst)
        {
            FuncInst = funcInst;
        }

        public void SetFuncInst(QsFuncInst funcInst)
        {
            FuncInst = funcInst;
        }
        
        public override void SetValue(QsValue fromValue)
        {
            FuncInst = ((QsFuncInstValue)fromValue).FuncInst;
        }

        public override QsValue MakeCopy()
        {
            // ReferenceCopy
            return new QsFuncInstValue(FuncInst);
        }
    }
    
    public class QsNullValue : QsValue
    {
        public static QsNullValue Instance { get; } = new QsNullValue();
        private QsNullValue() { }

        public override void SetValue(QsValue fromValue)
        {
            if (!(fromValue is QsNullValue))
                throw new InvalidOperationException(); 
        }

        public override QsValue MakeCopy()
        {
            return Instance;
        }        
    }

    // void 
    public class QsVoidValue : QsValue
    {
        public static QsVoidValue Instance { get; } = new QsVoidValue();
        private QsVoidValue() { }        
        
        public override QsValue MakeCopy()
        {
            throw new InvalidOperationException();
        }

        public override void SetValue(QsValue fromValue)
        {
            throw new InvalidOperationException();
        }
    }

    public abstract class QsObject
    {
        public virtual QsTypeInst GetTypeInst()
        {
            throw new NotImplementedException();
        }

        public virtual QsValue GetMemberValue(Name varName)
        {
            throw new NotImplementedException();
        }

        protected static TObject GetObject<TObject>(QsValue value) where TObject : QsObject
        {
            return (TObject)((QsObjectValue)value).Object!;
        }
    }    
   
    public class QsObjectValue : QsValue
    {
        public QsObject? Object { get; private set; }

        public QsObjectValue(QsObject? obj)
        {
            Object = obj;
        }
        
        public QsValue GetMemberValue(Name varName)
        {
            return Object!.GetMemberValue(varName);
        }

        public override QsValue MakeCopy()
        {
            return new QsObjectValue(Object);
        }

        public override void SetValue(QsValue fromValue)
        {
            Object = ((QsObjectValue)fromValue).Object;
        }

        public void SetObject(QsObject obj)
        {
            Object = obj;
        }

        public QsTypeInst GetTypeInst()
        {
            // 초기화 전에는 null일 수 있는데 타입체커를 통과하고 나면 호출하지 않을 것이다
            return Object!.GetTypeInst();
        }

        public override bool Equals(object? obj)
        {
            return obj is QsObjectValue value &&
                   EqualityComparer<QsObject?>.Default.Equals(Object, value.Object);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Object);
        }

        public static bool operator ==(QsObjectValue? left, QsObjectValue? right)
        {
            return EqualityComparer<QsObjectValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsObjectValue? left, QsObjectValue? right)
        {
            return !(left == right);
        }
    }
    
}

