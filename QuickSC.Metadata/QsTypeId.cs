using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{

    // (System.Runtime, System.X<,>.Y<,,>.T)
    public struct QsTypeId
    {
        public string ModuleName { get; }                    // 어느 모듈에서 온 것인가
        public ImmutableArray<QsNameElem> Elems { get; }  // 

        public QsTypeId(string moduleName, ImmutableArray<QsNameElem> elems)
        {
            ModuleName = moduleName;
            Elems = elems;
        }

        public QsTypeId(string moduleName, params QsNameElem[] elems)
        {
            ModuleName = moduleName;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeId id &&
                   ModuleName == id.ModuleName &&
                   QsSeqEqComparer.Equals(Elems, id.Elems);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(ModuleName);
            QsSeqEqComparer.AddHash(ref hashCode, Elems);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsTypeId left, QsTypeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsTypeId left, QsTypeId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append($"[{ModuleName}]");

            sb.AppendJoin('.', Elems);

            return sb.ToString();
        }
    }
}