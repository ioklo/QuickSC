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
        
        public override bool IsType(QsTypeInst typeInst)
        {
            // struct에서  is 류가 사용가능하게 해야할 수 도 있다.
            return false;
        }

        public override QsValue GetMemberValue(QsVarId varId)
        {
            throw new InvalidOperationException();
        }
    }

}
