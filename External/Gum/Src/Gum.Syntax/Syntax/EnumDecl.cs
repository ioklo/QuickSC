﻿using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Gum.Syntax
{
    public class EnumDeclElement : ISyntaxNode
    {
        public string Name { get; }
        public ImmutableArray<TypeAndName> Params { get; }        

        public EnumDeclElement(string name, ImmutableArray<TypeAndName> parameters)
        {
            Name = name;
            Params = parameters;
        }

        public EnumDeclElement(string name, params TypeAndName[] parameters)
        {
            Name = name;
            Params = ImmutableArray.Create(parameters);
        }

        public override bool Equals(object? obj)
        {
            return obj is EnumDeclElement element &&
                   Name == element.Name &&
                   Enumerable.SequenceEqual(Params, element.Params);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Params);
        }

        public static bool operator ==(EnumDeclElement? left, EnumDeclElement? right)
        {
            return EqualityComparer<EnumDeclElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(EnumDeclElement? left, EnumDeclElement? right)
        {
            return !(left == right);
        }
    }

    public class EnumDecl : ISyntaxNode
    {
        public string Name { get; }
        public ImmutableArray<string> TypeParams { get; }
        public ImmutableArray<EnumDeclElement> Elems { get; }
        public EnumDecl(string name, ImmutableArray<string> typeParams, ImmutableArray<EnumDeclElement> elems)
        {
            Name = name;
            TypeParams = typeParams;
            Elems = elems;
        }

        public EnumDecl(string name, ImmutableArray<string> typeParams, params EnumDeclElement[] elems)
        {
            Name = name;
            TypeParams = typeParams;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is EnumDecl decl &&
                   Name == decl.Name &&
                   Enumerable.SequenceEqual(TypeParams, decl.TypeParams) && 
                   Enumerable.SequenceEqual(Elems, decl.Elems);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Elems);
        }

        public static bool operator ==(EnumDecl? left, EnumDecl? right)
        {
            return EqualityComparer<EnumDecl?>.Default.Equals(left, right);
        }

        public static bool operator !=(EnumDecl? left, EnumDecl? right)
        {
            return !(left == right);
        }
    }
}
