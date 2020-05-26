using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    public struct QsFuncId
    {
        public string? ModuleName { get; }
        public ImmutableArray<QsNameElem> Elems { get; }

        public QsFuncId(string? moduleName, ImmutableArray<QsNameElem> elems)
        {
            ModuleName = moduleName;
            Elems = elems;
        }

        public QsFuncId(string? moduleName, params QsNameElem[] elems)
        {
            ModuleName = moduleName;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncId id &&
                   ModuleName == id.ModuleName &&
                   QsSeqEqComparer.Equals(Elems, id.Elems);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(ModuleName);
            QsSeqEqComparer.AddHash(ref hashCode, Elems);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsFuncId left, QsFuncId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsFuncId left, QsFuncId right)
        {
            return !(left == right);
        }
    }
}
