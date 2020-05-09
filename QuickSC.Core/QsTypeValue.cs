using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public class QsTypeValue
    {
        QsTypeValue outer;
        QsType type;
        ImmutableArray<QsTypeValue> argTypes;

        public QsTypeValue(QsTypeValue outer, QsType type, ImmutableArray<QsTypeValue> argTypes)
        {
            this.outer = outer;
            this.type = type;
            this.argTypes = argTypes;
        }

        public QsTypeValue? GetBaseTypeValue()
        {
            // class X<T, U> : P<T>.Base<U>
            // 
            // (X<,>, [int, short]) : ((null, P<>, int), Base<>, short)
            // 
            // (X<,>).GetBaseTypeValue() => ((null, P<>, T), Base<>, U) => 

            var baseTypeValue = type.GetBaseTypeValue();
        }

        public QsTypeValue? GetMemberTypeValue(string memberName, ImmutableArray<QsTypeValue> memberArgTypes)
        {
            // class X<T>
            // {
            //     class MyList<U> 
            //     {
            //     }
            // }
            // 
            // X<int>.MyList<short> list;
            // 
            // ((null, X<T>, [int]).GetMemberTypeValue("MyList", [short]) => 
            //     ((null, X<T>, [int]), MyList<U>, [short])
            // 
            // X<T>.GetMemberType("MyList") => MyList<U>
            // 
            var memberType = type.GetMemberType(memberName);
            if (memberType == null) return null;

            if (memberType.GetTypeParamCount() != memberArgTypes.Length) 
                return null;

            return new QsTypeValue(this, memberType, memberArgTypes);
        }
    }
}
