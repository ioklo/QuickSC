using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    // String 
    public class QsStringObject : QsObject
    {
        QsTypeInst typeInst;
        public string Data { get; } // 내부 구조는 string

        public QsStringObject(QsTypeInst typeInst, string data)
        {
            this.typeInst = typeInst;
            Data = data;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsStringObject @object &&
                   EqualityComparer<QsTypeInst>.Default.Equals(typeInst, @object.typeInst) &&
                   Data == @object.Data;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeInst, Data);
        }

        public static bool operator ==(QsStringObject? left, QsStringObject? right)
        {
            return EqualityComparer<QsStringObject?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStringObject? left, QsStringObject? right)
        {
            return !(left == right);
        }
    }
}
