using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{
    public abstract class QsTypeExp : IQsSyntaxNode
    {   
    }
 
    public class QsIdTypeExp : QsTypeExp
    {
        public string Name { get; }
        public ImmutableArray<QsTypeExp> TypeArgs { get; }
        public QsIdTypeExp(string name, ImmutableArray<QsTypeExp> typeArgs) { Name = name; TypeArgs = typeArgs; }
        public QsIdTypeExp(string name, params QsTypeExp[] typeArgs) { Name = name; TypeArgs = ImmutableArray.Create(typeArgs); }

        public override bool Equals(object? obj)
        {
            return obj is QsIdTypeExp exp &&
                   Name == exp.Name &&
                   Enumerable.SequenceEqual(TypeArgs, exp.TypeArgs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, TypeArgs);
        }

        public static bool operator ==(QsIdTypeExp? left, QsIdTypeExp? right)
        {
            return EqualityComparer<QsIdTypeExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIdTypeExp? left, QsIdTypeExp? right)
        {
            return !(left == right);
        }
    }

    public class QsMemberTypeExp : QsTypeExp
    {
        public QsTypeExp Parent { get; }        
        public string MemberName { get; }
        public ImmutableArray<QsTypeExp> TypeArgs { get; }

        public QsMemberTypeExp(QsTypeExp parent, string memberName, ImmutableArray<QsTypeExp> typeArgs)
        {
            Parent = parent;
            MemberName = memberName;
            TypeArgs = typeArgs;
        }
    }
}