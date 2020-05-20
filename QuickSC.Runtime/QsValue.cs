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
        public abstract void SetValue(QsValue v);
        public abstract QsValue MakeCopy();

        // 뭘 리턴해야 하는거냐
        public abstract QsValue GetMemberValue(QsVarId varId);

        public abstract bool IsType(QsTypeInst typeInst);
    }
    
    public class QsEnumValue : QsValue
    {
        public QsTypeInst TypeInst { get; }
        ImmutableDictionary<QsVarId, QsValue> values;

        public QsEnumValue(QsTypeInst typeInst, ImmutableDictionary<QsVarId, QsValue> values)
        {
            TypeInst = typeInst;
            this.values = values;
        }
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            return values[varId];
        }

        public override QsValue MakeCopy()
        {
            var newValues = ImmutableDictionary.CreateBuilder<QsVarId, QsValue>();

            foreach (var v in values)
                newValues.Add(v.Key, v.Value.MakeCopy());

            return new QsEnumValue(TypeInst, newValues.ToImmutable());
        }

        public override void SetValue(QsValue v)
        {
            this.values = ((QsEnumValue)v).values;
        }

        public override bool IsType(QsTypeInst typeInst)
        {
            QsTypeInst? curTypeInst = TypeInst;

            while(curTypeInst != null)
            {
                if (curTypeInst == typeInst) return true;
                curTypeInst = curTypeInst.GetBaseTypeInst();
            }

            return false;
        }
    }

    public class QsFuncInstValue : QsValue
    {
        public QsFuncInst FuncInst { get; private set; }

        public QsFuncInstValue(QsFuncInst funcInst)
        {
            FuncInst = funcInst;
        }

        public override void SetValue(QsValue v)
        {
            FuncInst = ((QsFuncInstValue)v).FuncInst;
        }

        public override QsValue MakeCopy()
        {
            throw new NotImplementedException();
        }
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            throw new NotImplementedException();
        }

        public override bool IsType(QsTypeInst typeInst)
        {
            throw new NotImplementedException();
        }
    }
    
    public class QsNullValue : QsValue
    {
        public static QsNullValue Instance { get; } = new QsNullValue();
        private QsNullValue() { }

        public override void SetValue(QsValue v)
        {
            if (!(v is QsNullValue))
                throw new InvalidOperationException(); 
        }

        public override QsValue MakeCopy()
        {
            return Instance;
        }
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            throw new InvalidOperationException();
        }

        public override bool IsType(QsTypeInst type)
        {
            return false;
        }
    }

    // void 
    public class QsVoidValue : QsValue
    {
        public static QsVoidValue Instance { get; } = new QsVoidValue();
        private QsVoidValue() { }
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            throw new InvalidOperationException();
        }

        public override bool IsType(QsTypeInst typeInst)
        {
            throw new NotImplementedException();
        }

        public override QsValue MakeCopy()
        {
            throw new InvalidOperationException();
        }

        public override void SetValue(QsValue v)
        {
            throw new InvalidOperationException();
        }
    }

    public abstract class QsObject
    {
        public virtual QsValue GetMemberValue(QsVarId varId)
        {
            throw new NotImplementedException();
        }

        protected static TObject GetObject<TObject>(QsValue value) where TObject : QsObject
        {
            return (TObject)((QsObjectValue)value).Object;
        }
    }    
   
    public class QsObjectValue : QsValue
    {
        public QsObject Object { get; private set; }

        public QsObjectValue(QsObject obj)
        {
            Object = obj;
        }
        
        public override QsValue GetMemberValue(QsVarId varId)
        {
            return Object.GetMemberValue(varId);
        }

        public override QsValue MakeCopy()
        {
            return new QsObjectValue(Object);
        }

        public override void SetValue(QsValue value)
        {
            Object = ((QsObjectValue)value).Object;
        }

        public override bool IsType(QsTypeInst type)
        {
            throw new NotImplementedException();
        }
    }
    
}

