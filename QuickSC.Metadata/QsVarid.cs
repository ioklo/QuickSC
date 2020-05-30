using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC
{
    // Global, Class Variable만 해당, Local은 해당되지 않는다
    public struct QsVarId
    {
        public string ModuleName { get; }
        public ImmutableArray<QsNameElem> Elems { get; }
        
        public QsVarId(string moduleName, ImmutableArray<QsNameElem> elems)
        {
            ModuleName = moduleName;
            Elems = elems;
        }

        public QsVarId(string moduleName, params QsNameElem[] elems)
        {
            ModuleName = moduleName;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarId id &&
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

        public static bool operator ==(QsVarId left, QsVarId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsVarId left, QsVarId right)
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
