using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Syntax
{
    public struct QsEnumDeclElement
    {
        public string Name { get; }
        public ImmutableArray<QsTypeAndName> Params { get; }        

        public QsEnumDeclElement(string name, ImmutableArray<QsTypeAndName> parameters)
        {
            Name = name;
            Params = parameters;
        }

        public QsEnumDeclElement(string name, params QsTypeAndName[] parameters)
        {
            Name = name;
            Params = ImmutableArray.Create(parameters);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumDeclElement element &&
                   Name == element.Name &&
                   Enumerable.SequenceEqual(Params, element.Params);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Params);
        }

        public static bool operator ==(QsEnumDeclElement left, QsEnumDeclElement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsEnumDeclElement left, QsEnumDeclElement right)
        {
            return !(left == right);
        }
    }

    public class QsEnumDecl
    {
        public string Name { get; }
        public ImmutableArray<QsEnumDeclElement> Elems { get; }
        public QsEnumDecl(string name, ImmutableArray<QsEnumDeclElement> elems)
        {
            Name = name;
            Elems = elems;
        }

        public QsEnumDecl(string name, params QsEnumDeclElement[] elems)
        {
            Name = name;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumDecl decl &&
                   Name == decl.Name &&
                   Enumerable.SequenceEqual(Elems, decl.Elems);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Elems);
        }

        public static bool operator ==(QsEnumDecl? left, QsEnumDecl? right)
        {
            return EqualityComparer<QsEnumDecl?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsEnumDecl? left, QsEnumDecl? right)
        {
            return !(left == right);
        }
    }
}
