using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.Runtime
{
    // String 
    public class QsStringBuildInfo : GumObject
    {
        TypeInst typeInst;
        public string Data { get; } // 내부 구조는 string

        public QsStringBuildInfo(TypeInst typeInst, string data)
        {
            this.typeInst = typeInst;
            Data = data;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsStringBuildInfo @object &&
                   EqualityComparer<TypeInst>.Default.Equals(typeInst, @object.typeInst) &&
                   Data == @object.Data;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(typeInst, Data);
        }

        public static bool operator ==(QsStringBuildInfo? left, QsStringBuildInfo? right)
        {
            return EqualityComparer<QsStringBuildInfo?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStringBuildInfo? left, QsStringBuildInfo? right)
        {
            return !(left == right);
        }

        public override TypeInst GetTypeInst()
        {
            return typeInst;
        }
    }
}
