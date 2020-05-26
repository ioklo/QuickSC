using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Syntax
{
    public class QsEnumDeclElement : IQsSyntaxNode
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

        public static bool operator ==(QsEnumDeclElement? left, QsEnumDeclElement? right)
        {
            return EqualityComparer<QsEnumDeclElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsEnumDeclElement? left, QsEnumDeclElement? right)
        {
            return !(left == right);
        }
    }

    public class QsEnumDecl : IQsSyntaxNode
    {
        public string Name { get; }
        public ImmutableArray<string> TypeParams { get; }
        public ImmutableArray<QsEnumDeclElement> Elems { get; }
        public QsEnumDecl(string name, ImmutableArray<string> typeParams, ImmutableArray<QsEnumDeclElement> elems)
        {
            Name = name;
            TypeParams = typeParams;
            Elems = elems;
        }

        public QsEnumDecl(string name, ImmutableArray<string> typeParams, params QsEnumDeclElement[] elems)
        {
            Name = name;
            TypeParams = typeParams;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsEnumDecl decl &&
                   Name == decl.Name &&
                   Enumerable.SequenceEqual(TypeParams, decl.TypeParams) && 
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
