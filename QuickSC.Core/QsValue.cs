using QuickSC.Syntax;
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
        public abstract bool SetValue(QsValue v);
        public abstract QsValue MakeCopy();

        // 뭘 리턴해야 하는거냐
        public abstract QsCallable? GetMemberFuncs(QsMemberFuncId funcId);
        public abstract QsValue? GetMemberValue(string varName);

        public abstract bool IsType(QsTypeInst typeInst);
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
            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            return null;
        }

        public override bool IsType(QsType type)
        {
            // struct에서  is 류가 사용가능하게 해야할 수 도 있다.
            return false;
        }
    }

    public class QsEnumValue : QsValue
    {
        public QsTypeInst TypeInst { get; }
        ImmutableDictionary<string, QsValue> values;

        public QsEnumValue(QsTypeInst typeInst, ImmutableDictionary<string, QsValue> values)
        {
            TypeInst = typeInst;
            this.values = values;
        }

        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            if (values.TryGetValue(varName, out var value))
                return value;

            return null;
        }

        public override QsValue MakeCopy()
        {
            var newValues = ImmutableDictionary.CreateBuilder<string, QsValue>();

            foreach (var v in values)
                newValues.Add(v.Key, v.Value.MakeCopy());

            return new QsEnumValue(TypeInst, newValues.ToImmutable());
        }

        public override bool SetValue(QsValue v)
        {
            if (v is QsEnumValue recordValue)
            {
                this.values = recordValue.values;
                return true;
            }

            return false;
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

    public class QsTypeValueBak
    {
        public QsType Type { get; private set; }

        public QsTypeValueBak(QsType type)
        {
            Type = type;
        }
        
        public override QsCallable? GetMemberFuncs(QsMemberFuncId funcId)
        {
            if( funcId.Kind == QsMemberFuncKind.Normal)
                return Type.GetMemberFuncs(funcId.Name);

            return null;
        }

        public override QsValue? GetMemberValue(string varName)
        {
            return Type.GetMemberValue(varName);
        }

        public sealed override bool IsType(QsType type)
        {
            return QsTypeType.Instance == type;
        }

        public override QsValue MakeCopy()
        {
            return new QsTypeValue(Type);
        }

        public override bool SetValue(QsValue v)
        {
            if (v is QsTypeValue tv)
            {
                Type = tv.Type;
                return true;
            }

            return false;
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

        public override bool IsType(QsType type)
        {
            return false;
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

        public override bool IsType(QsType type)
        {
            throw new NotImplementedException();
        }
    }
    
}

