using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{
    // <Async> <RetTypeName> <FuncName> <LPAREN> <ARGS> <RPAREN>
    // LBRACE>
    // [Stmt]
    // <RBRACE>
    // a(b, params c, d);
    // a<T>(int b, params T x, int d);
    public class QsFuncDecl
    {
        public QsTypeExp RetType { get; }
        public string Name { get; }
        public ImmutableArray<QsFuncDeclParam> Params { get; }
        public int? VariadicParamIndex { get; } 
        public QsBlockStmt Body { get; }

        public QsFuncDecl(QsTypeExp retType, string name, ImmutableArray<QsFuncDeclParam> parameters, int? variadicParamIndex, QsBlockStmt body)
        {
            RetType = retType;
            Name = name;
            Params = parameters;
            VariadicParamIndex = variadicParamIndex;
            Body = body;
        }

        public QsFuncDecl(QsTypeExp retType, string name, int? variadicParamIndex, QsBlockStmt body, params QsFuncDeclParam[] parameters)
        {
            RetType = retType;
            Name = name;
            Params = ImmutableArray.Create(parameters);
            VariadicParamIndex = variadicParamIndex;
            Body = body;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncDecl decl &&
                   EqualityComparer<QsTypeExp>.Default.Equals(RetType, decl.RetType) &&
                   Name == decl.Name &&
                   Enumerable.SequenceEqual(Params, decl.Params) &&
                   VariadicParamIndex == decl.VariadicParamIndex &&
                   EqualityComparer<QsBlockStmt>.Default.Equals(Body, decl.Body);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RetType, Name, Params, VariadicParamIndex, Body);
        }

        public static bool operator ==(QsFuncDecl? left, QsFuncDecl? right)
        {
            return EqualityComparer<QsFuncDecl?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncDecl? left, QsFuncDecl? right)
        {
            return !(left == right);
        }
    }
}