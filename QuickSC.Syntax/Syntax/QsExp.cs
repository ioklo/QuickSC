using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsExp
    {
    }
    
    public class QsIdentifierExp : QsExp
    {
        public string Value;
        public QsIdentifierExp(string value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIdentifierExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIdentifierExp? left, QsIdentifierExp? right)
        {
            return EqualityComparer<QsIdentifierExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIdentifierExp? left, QsIdentifierExp? right)
        {
            return !(left == right);
        }
    }

    public class QsStringExp : QsExp
    {
        public ImmutableArray<QsStringExpElement> Elements { get; }
        
        public QsStringExp(ImmutableArray<QsStringExpElement> elements)
        {
            Elements = elements;
        }

        public QsStringExp(params QsStringExpElement[] elements)
        {
            Elements = ImmutableArray.Create(elements);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsStringExp exp && Enumerable.SequenceEqual(Elements, exp.Elements);                   
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            foreach (var elem in Elements)
                hashCode.Add(elem);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsStringExp? left, QsStringExp? right)
        {
            return EqualityComparer<QsStringExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStringExp? left, QsStringExp? right)
        {
            return !(left == right);
        }
    }

    public class QsIntLiteralExp : QsExp
    {
        public int Value { get; }
        public QsIntLiteralExp(int value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsIntLiteralExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsIntLiteralExp? left, QsIntLiteralExp? right)
        {
            return EqualityComparer<QsIntLiteralExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIntLiteralExp? left, QsIntLiteralExp? right)
        {
            return !(left == right);
        }
    }

    public class QsBoolLiteralExp : QsExp
    {
        public bool Value { get; }
        public QsBoolLiteralExp(bool value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsBoolLiteralExp exp &&
                   Value == exp.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsBoolLiteralExp? left, QsBoolLiteralExp? right)
        {
            return EqualityComparer<QsBoolLiteralExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBoolLiteralExp? left, QsBoolLiteralExp? right)
        {
            return !(left == right);
        }
    }

    public class QsBinaryOpExp : QsExp
    {
        public QsBinaryOpKind Kind { get; }
        public QsExp Operand0 { get; }
        public QsExp Operand1 { get; }
        
        public QsBinaryOpExp(QsBinaryOpKind kind, QsExp operand0, QsExp operand1)
        {
            Kind = kind;
            Operand0 = operand0;
            Operand1 = operand1;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsBinaryOpExp exp &&
                   Kind == exp.Kind &&
                   EqualityComparer<QsExp>.Default.Equals(Operand0, exp.Operand0) &&
                   EqualityComparer<QsExp>.Default.Equals(Operand1, exp.Operand1);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Operand0, Operand1);
        }

        public static bool operator ==(QsBinaryOpExp? left, QsBinaryOpExp? right)
        {
            return EqualityComparer<QsBinaryOpExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBinaryOpExp? left, QsBinaryOpExp? right)
        {
            return !(left == right);
        }
    }

    public class QsUnaryOpExp : QsExp
    {
        public QsUnaryOpKind Kind { get; }
        public QsExp OperandExp{ get; }
        public QsUnaryOpExp(QsUnaryOpKind kind, QsExp operandExp)
        {
            Kind = kind;
            OperandExp = operandExp;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsUnaryOpExp exp &&
                   Kind == exp.Kind &&
                   EqualityComparer<QsExp>.Default.Equals(OperandExp, exp.OperandExp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, OperandExp);
        }

        public static bool operator ==(QsUnaryOpExp? left, QsUnaryOpExp? right)
        {
            return EqualityComparer<QsUnaryOpExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsUnaryOpExp? left, QsUnaryOpExp? right)
        {
            return !(left == right);
        }
    }

    public abstract class QsCallExpCallable
    {
    }

    public class QsFuncCallExpCallable : QsCallExpCallable
    {
        public QsFuncDecl FuncDecl { get; }
        public QsFuncCallExpCallable(QsFuncDecl funcDecl) { FuncDecl = funcDecl; }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncCallExpCallable callable &&
                   EqualityComparer<QsFuncDecl>.Default.Equals(FuncDecl, callable.FuncDecl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FuncDecl);
        }

        public static bool operator ==(QsFuncCallExpCallable? left, QsFuncCallExpCallable? right)
        {
            return EqualityComparer<QsFuncCallExpCallable?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncCallExpCallable? left, QsFuncCallExpCallable? right)
        {
            return !(left == right);
        }
    }

    public class QsExpCallExpCallable : QsCallExpCallable
    {
        public QsExp Exp { get; }
        public QsExpCallExpCallable(QsExp exp) { Exp = exp; }

        public override bool Equals(object? obj)
        {
            return obj is QsExpCallExpCallable callable &&
                   EqualityComparer<QsExp>.Default.Equals(Exp, callable.Exp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Exp);
        }

        public static bool operator ==(QsExpCallExpCallable? left, QsExpCallExpCallable? right)
        {
            return EqualityComparer<QsExpCallExpCallable?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsExpCallExpCallable? left, QsExpCallExpCallable? right)
        {
            return !(left == right);
        }
    }

    // MemberCallExp는 따로 
    public class QsCallExp : QsExp
    {
        public QsCallExpCallable Callable { get; }

        // TODO: params, out, 등 처리를 하려면 QsExp가 아니라 다른거여야 한다
        public ImmutableArray<QsExp> Args { get; }

        public QsCallExp(QsCallExpCallable callable, ImmutableArray<QsExp> args)
        {
            Callable = callable;
            Args = args;
        }

        public QsCallExp(QsCallExpCallable callable, params QsExp[] args)
        {
            Callable = callable;
            Args = ImmutableArray.Create(args);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsCallExp exp &&
                   EqualityComparer<QsCallExpCallable>.Default.Equals(Callable, exp.Callable) &&
                   Enumerable.SequenceEqual(Args, exp.Args);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Callable, Args);
        }

        public static bool operator ==(QsCallExp? left, QsCallExp? right)
        {
            return EqualityComparer<QsCallExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsCallExp? left, QsCallExp? right)
        {
            return !(left == right);
        }
    }

    public struct QsLambdaExpParam
    {
        public QsTypeExp? Type { get; }
        public string Name { get; }

        public QsLambdaExpParam(QsTypeExp? type, string name)
        {
            Type = type;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsLambdaExpParam param &&
                   EqualityComparer<QsTypeExp?>.Default.Equals(Type, param.Type) &&
                   Name == param.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Name);
        }

        public static bool operator ==(QsLambdaExpParam left, QsLambdaExpParam right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsLambdaExpParam left, QsLambdaExpParam right)
        {
            return !(left == right);
        }
    }

    public class QsLambdaExp : QsExp
    {
        public QsFuncKind Kind { get; }
        public ImmutableArray<QsLambdaExpParam> Params { get; }
        public QsStmt Body { get; }

        public QsLambdaExp(QsFuncKind kind, ImmutableArray<QsLambdaExpParam> parameters, QsStmt body)
        {
            Kind = kind;
            Params = parameters;
            Body = body;
        }

        public QsLambdaExp(QsFuncKind kind, QsStmt body, params QsLambdaExpParam[] parameters)
        {
            Params = ImmutableArray.Create(parameters);
            Body = body;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsLambdaExp exp &&
                   Enumerable.SequenceEqual(Params, exp.Params) &&
                   EqualityComparer<QsStmt>.Default.Equals(Body, exp.Body);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Params, Body);
        }

        public static bool operator ==(QsLambdaExp? left, QsLambdaExp? right)
        {
            return EqualityComparer<QsLambdaExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsLambdaExp? left, QsLambdaExp? right)
        {
            return !(left == right);
        }
    }
}
