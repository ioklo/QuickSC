using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace QuickSC.Syntax
{
    public abstract class QsExp : IQsSyntaxNode
    {
    }
    
    public class QsIdentifierExp : QsExp
    {
        public string Value { get; }
        public ImmutableArray<QsTypeExp> TypeArgs { get; }
        public QsIdentifierExp(string value, ImmutableArray<QsTypeExp> typeArgs) { Value = value; TypeArgs = typeArgs; }
        public QsIdentifierExp(string value, params QsTypeExp[] typeArgs) { Value = value; TypeArgs = ImmutableArray.Create(typeArgs); }

        public override bool Equals(object? obj)
        {
            return obj is QsIdentifierExp exp &&
                   Value == exp.Value &&
                   Enumerable.SequenceEqual(TypeArgs, exp.TypeArgs);
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
        public QsExp Operand{ get; }
        public QsUnaryOpExp(QsUnaryOpKind kind, QsExp operand)
        {
            Kind = kind;
            Operand = operand;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsUnaryOpExp exp &&
                   Kind == exp.Kind &&
                   EqualityComparer<QsExp>.Default.Equals(Operand, exp.Operand);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, Operand);
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

    // MemberCallExp는 따로 
    public class QsCallExp : QsExp
    {
        public QsExp Callable { get; }

        public ImmutableArray<QsTypeExp> TypeArgs { get; }

        // TODO: params, out, 등 처리를 하려면 QsExp가 아니라 다른거여야 한다
        public ImmutableArray<QsExp> Args { get; }

        public QsCallExp(QsExp callable, ImmutableArray<QsTypeExp> typeArgs, ImmutableArray<QsExp> args)
        {
            Callable = callable;
            TypeArgs = typeArgs;
            Args = args;
        }

        public QsCallExp(QsExp callable, ImmutableArray<QsTypeExp> typeArgs, params QsExp[] args)
        {
            Callable = callable;
            TypeArgs = typeArgs;
            Args = ImmutableArray.Create(args);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsCallExp exp &&
                   EqualityComparer<QsExp>.Default.Equals(Callable, exp.Callable) &&
                   Enumerable.SequenceEqual(TypeArgs, exp.TypeArgs) &&
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
        public ImmutableArray<QsLambdaExpParam> Params { get; }
        public QsStmt Body { get; }

        public QsLambdaExp(ImmutableArray<QsLambdaExpParam> parameters, QsStmt body)
        {
            Params = parameters;
            Body = body;
        }

        public QsLambdaExp(QsStmt body, params QsLambdaExpParam[] parameters)
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
    
    // a[b]
    public class QsIndexerExp : QsExp
    {
        public QsExp Object { get; }
        public QsExp Index { get; }

        public QsIndexerExp(QsExp obj, QsExp index)
        {
            Object = obj;
            Index = index;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsIndexerExp exp &&
                   EqualityComparer<QsExp>.Default.Equals(Object, exp.Object) &&
                   EqualityComparer<QsExp>.Default.Equals(Index, exp.Index);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Object, Index);
        }

        public static bool operator ==(QsIndexerExp? left, QsIndexerExp? right)
        {
            return EqualityComparer<QsIndexerExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIndexerExp? left, QsIndexerExp? right)
        {
            return !(left == right);
        }
    }

    public class QsMemberCallExp : QsExp
    {
        public QsExp Object { get; }
        public string MemberName { get; }
        public ImmutableArray<QsTypeExp> MemberTypeArgs { get; }
        public ImmutableArray<QsExp> Args { get; }

        public QsMemberCallExp(QsExp obj, string memberName, ImmutableArray<QsTypeExp> typeArgs, ImmutableArray<QsExp> args)
        {
            Object = obj;
            MemberName = memberName;
            MemberTypeArgs = typeArgs;
            Args = args;
        }

        public QsMemberCallExp(QsExp obj, string memberName, ImmutableArray<QsTypeExp> typeArgs, params QsExp[] args)
        {
            Object = obj;
            MemberName = memberName;
            MemberTypeArgs = typeArgs;
            Args = ImmutableArray.Create(args);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsMemberCallExp exp &&
                   EqualityComparer<QsExp>.Default.Equals(Object, exp.Object) &&
                   MemberName == exp.MemberName &&
                   Enumerable.SequenceEqual(MemberTypeArgs, exp.MemberTypeArgs) &&
                   Enumerable.SequenceEqual(Args, exp.Args);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Object, MemberName, Args);
        }

        public static bool operator ==(QsMemberCallExp? left, QsMemberCallExp? right)
        {
            return EqualityComparer<QsMemberCallExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsMemberCallExp? left, QsMemberCallExp? right)
        {
            return !(left == right);
        }
    }

    public class QsMemberExp : QsExp
    {
        public QsExp Object { get; }
        public string MemberName { get; }
        public ImmutableArray<QsTypeExp> MemberTypeArgs { get; }

        public QsMemberExp(QsExp obj, string memberName, ImmutableArray<QsTypeExp> memberTypeArgs)
        {
            Object = obj;
            MemberName = memberName;
            MemberTypeArgs = memberTypeArgs;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsMemberExp exp &&
                   EqualityComparer<QsExp>.Default.Equals(Object, exp.Object) &&
                   MemberName == exp.MemberName &&
                   Enumerable.SequenceEqual(MemberTypeArgs, exp.MemberTypeArgs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Object, MemberName);
        }

        public static bool operator ==(QsMemberExp? left, QsMemberExp? right)
        {
            return EqualityComparer<QsMemberExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsMemberExp? left, QsMemberExp? right)
        {
            return !(left == right);
        }
    }

    public class QsListExp : QsExp
    {
        QsTypeExp? ElemType { get; }
        public ImmutableArray<QsExp> Elems { get; }

        public QsListExp(QsTypeExp? elemType, ImmutableArray<QsExp> elems)
        {
            ElemType = elemType;
            Elems = elems;
        }

        public QsListExp(QsTypeExp? elemType, params QsExp[] elems)
        {
            ElemType = elemType;
            Elems = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsListExp exp &&
                   EqualityComparer<QsTypeExp?>.Default.Equals(ElemType, exp.ElemType) &&
                   Enumerable.SequenceEqual(Elems, exp.Elems);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Elems);
        }

        public static bool operator ==(QsListExp? left, QsListExp? right)
        {
            return EqualityComparer<QsListExp?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsListExp? left, QsListExp? right)
        {
            return !(left == right);
        }
    }
}
