using QuickSC;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    // runtime placeholder
    public abstract class QsValue
    {
        public abstract void SetValue(QsValue fromValue);
        public abstract QsValue MakeCopy();

        // 뭘 리턴해야 하는거냐
        public abstract QsValue GetMemberValue(QsMetaItemId varId);
        public abstract QsTypeInst GetTypeInst();
    }
    
    public class QsEnumValue : QsValue
    {
        public QsTypeInst TypeInst { get; }
        ImmutableDictionary<QsMetaItemId, QsValue> values;

        public QsEnumValue(QsTypeInst typeInst, ImmutableDictionary<QsMetaItemId, QsValue> values)
        {
            TypeInst = typeInst;
            this.values = values;
        }
        
        public override QsValue GetMemberValue(QsMetaItemId varId)
        {
            return values[varId];
        }

        public override QsValue MakeCopy()
        {
            var newValues = ImmutableDictionary.CreateBuilder<QsMetaItemId, QsValue>();

            foreach (var v in values)
                newValues.Add(v.Key, v.Value.MakeCopy());

            return new QsEnumValue(TypeInst, newValues.ToImmutable());
        }

        public override void SetValue(QsValue fromValue)
        {
            this.values = ((QsEnumValue)fromValue).values;
        }

        public override QsTypeInst GetTypeInst()
        {
            return TypeInst;
        }
    }

    public class QsFuncInstValue : QsValue
    {
        public QsFuncInst FuncInst { get; private set; }

        public QsFuncInstValue(QsFuncInst funcInst)
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
        
        public override QsValue GetMemberValue(QsMetaItemId varId)
        {
            throw new NotImplementedException();
        }

        public override QsTypeInst GetTypeInst()
        {
            throw new NotImplementedException();
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
        
        public override QsValue GetMemberValue(QsMetaItemId varId)
        {
            throw new InvalidOperationException();
        }

        public override QsTypeInst GetTypeInst()
        {
            throw new InvalidOperationException();
        }
    }

    // void 
    public class QsVoidValue : QsValue
    {
        public static QsVoidValue Instance { get; } = new QsVoidValue();
        private QsVoidValue() { }
        
        public override QsValue GetMemberValue(QsMetaItemId varId)
        {
            throw new InvalidOperationException();
        }

        public override QsTypeInst GetTypeInst()
        {
            throw new NotImplementedException();
        }

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

        public virtual QsValue GetMemberValue(QsMetaItemId varId)
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
        
        public override QsValue GetMemberValue(QsMetaItemId varId)
        {
            return Object!.GetMemberValue(varId);
        }

        public override QsValue MakeCopy()
        {
            return new QsObjectValue(Object);
        }

        public override void SetValue(QsValue fromValue)
        {
            Object = ((QsObjectValue)fromValue).Object;
        }

        public override QsTypeInst GetTypeInst()
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

