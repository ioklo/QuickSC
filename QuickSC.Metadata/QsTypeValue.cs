using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC
{
    // enum류 naming을 바꿔볼까
    public abstract class QsTypeValue
    {        
    }

    // "var"
    public class QsTypeValue_Var : QsTypeValue
    {
        public static QsTypeValue_Var Instance { get; } = new QsTypeValue_Var();
        private QsTypeValue_Var() { }
    }

    // T
    public class QsTypeValue_TypeVar : QsTypeValue
    {        
        public QsMetaItemId ParentId { get; } 
        public string Name { get; }

        public QsTypeValue_TypeVar(QsMetaItemId parentId, string name)
        {
            ParentId = parentId;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeValue_TypeVar value &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(ParentId, value.ParentId) &&
                   Name == value.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ParentId, Name);
        }

        public static bool operator ==(QsTypeValue_TypeVar? left, QsTypeValue_TypeVar? right)
        {
            return EqualityComparer<QsTypeValue_TypeVar?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeValue_TypeVar? left, QsTypeValue_TypeVar? right)
        {
            return !(left == right);
        }
    }

    public class QsTypeValue_Normal : QsTypeValue
    {
        // TODO: Outer를 NormalTypeValue로 고쳐보기
        public QsTypeValue? Outer { get; }
        public QsMetaItemId TypeId { get; }
        public ImmutableArray<QsTypeValue> TypeArgs { get; }

        public QsTypeValue_Normal(QsTypeValue? outer, QsMetaItemId typeId, ImmutableArray<QsTypeValue> typeArgs)
        {
            this.Outer = outer;
            this.TypeId = typeId;
            this.TypeArgs = typeArgs;
        }

        public QsTypeValue_Normal(QsTypeValue? outer, QsMetaItemId TypeId, params QsTypeValue[] typeArgs)
        {
            this.Outer = outer;
            this.TypeId = TypeId;
            this.TypeArgs = ImmutableArray.Create(typeArgs);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeValue_Normal value &&
                   EqualityComparer<QsTypeValue?>.Default.Equals(Outer, value.Outer) &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(TypeId, value.TypeId) &&
                   Enumerable.SequenceEqual(TypeArgs, value.TypeArgs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Outer, TypeId, TypeArgs);
        }

        public static bool operator ==(QsTypeValue_Normal? left, QsTypeValue_Normal? right)
        {
            return EqualityComparer<QsTypeValue_Normal?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeValue_Normal? left, QsTypeValue_Normal? right)
        {
            return !(left == right);
        }
    }

    // "void"
    public class QsTypeValue_Void : QsTypeValue
    {
        public static QsTypeValue_Void Instance { get; } = new QsTypeValue_Void();
        private QsTypeValue_Void() { }
    }

    // ArgTypeValues => RetValueTypes
    public class QsTypeValue_Func : QsTypeValue
    {
        public QsTypeValue Return { get; }
        public ImmutableArray<QsTypeValue> Params { get; }

        public QsTypeValue_Func(QsTypeValue ret, ImmutableArray<QsTypeValue> parameters)
        {
            Return = ret;
            Params = parameters;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeValue_Func value &&
                   EqualityComparer<QsTypeValue>.Default.Equals(Return, value.Return) &&
                   Enumerable.SequenceEqual(Params, value.Params);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Return, Params);
        }

        public static bool operator ==(QsTypeValue_Func? left, QsTypeValue_Func? right)
        {
            return EqualityComparer<QsTypeValue_Func?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeValue_Func? left, QsTypeValue_Func? right)
        {
            return !(left == right);
        }
    }
}
