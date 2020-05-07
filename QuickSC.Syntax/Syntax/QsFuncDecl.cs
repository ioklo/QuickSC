using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{
    // <RetTypeName> <FuncName> <LPAREN> <ARGS> <RPAREN>
    // LBRACE>
    // [Stmt]
    // <RBRACE>
    // a(b, params c, d);
    // a<T>(int b, params T x, int d);
    public class QsFuncDecl
    {
        public QsFuncKind FuncKind { get; }
        public QsTypeExp RetType { get; }
        public string Name { get; }
        public ImmutableArray<QsTypeAndName> Params { get; }
        public int? VariadicParamIndex { get; } 
        public QsBlockStmt Body { get; }

        public QsFuncDecl(QsFuncKind funcKind, QsTypeExp retType, string name, ImmutableArray<QsTypeAndName> parameters, int? variadicParamIndex, QsBlockStmt body)
        {
            FuncKind = funcKind;
            RetType = retType;
            Name = name;
            Params = parameters;
            VariadicParamIndex = variadicParamIndex;
            Body = body;
        }

        public QsFuncDecl(QsFuncKind funcKind, QsTypeExp retType, string name, int? variadicParamIndex, QsBlockStmt body, params QsTypeAndName[] parameters)
        {
            FuncKind = funcKind;
            RetType = retType;
            Name = name;
            Params = ImmutableArray.Create(parameters);
            VariadicParamIndex = variadicParamIndex;
            Body = body;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncDecl decl &&                   
                   FuncKind == decl.FuncKind &&
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