using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC
{
    public abstract class QsTypeValue
    {
    }

    // "var"
    public class QsVarTypeValue : QsTypeValue
    {
        public static QsVarTypeValue Instance { get; } = new QsVarTypeValue();
        private QsVarTypeValue() { }
    }

    // T
    public class QsTypeVarTypeValue : QsTypeValue
    {        
        public QsMetaItemId ParentId { get; } 
        public string Name { get; }

        public QsTypeVarTypeValue(QsMetaItemId parentId, string name)
        {
            ParentId = parentId;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeVarTypeValue value &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(ParentId, value.ParentId) &&
                   Name == value.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ParentId, Name);
        }

        public static bool operator ==(QsTypeVarTypeValue? left, QsTypeVarTypeValue? right)
        {
            return EqualityComparer<QsTypeVarTypeValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeVarTypeValue? left, QsTypeVarTypeValue? right)
        {
            return !(left == right);
        }
    }

    public class QsNormalTypeValue : QsTypeValue
    {
        // TODO: Outer를 NormalTypeValue로 고쳐보기
        public QsTypeValue? Outer { get; }
        public QsMetaItemId TypeId { get; }
        public ImmutableArray<QsTypeValue> TypeArgs { get; }

        public QsNormalTypeValue(QsTypeValue? outer, QsMetaItemId typeId, ImmutableArray<QsTypeValue> typeArgs)
        {
            this.Outer = outer;
            this.TypeId = typeId;
            this.TypeArgs = typeArgs;
        }

        public QsNormalTypeValue(QsTypeValue? outer, QsMetaItemId TypeId, params QsTypeValue[] typeArgs)
        {
            this.Outer = outer;
            this.TypeId = TypeId;
            this.TypeArgs = ImmutableArray.Create(typeArgs);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsNormalTypeValue value &&
                   EqualityComparer<QsTypeValue?>.Default.Equals(Outer, value.Outer) &&
                   EqualityComparer<QsMetaItemId>.Default.Equals(TypeId, value.TypeId) &&
                   Enumerable.SequenceEqual(TypeArgs, value.TypeArgs);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Outer, TypeId, TypeArgs);
        }

        public static bool operator ==(QsNormalTypeValue? left, QsNormalTypeValue? right)
        {
            return EqualityComparer<QsNormalTypeValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsNormalTypeValue? left, QsNormalTypeValue? right)
        {
            return !(left == right);
        }
    }

    // "void"
    public class QsVoidTypeValue : QsTypeValue
    {
        public static QsVoidTypeValue Instance { get; } = new QsVoidTypeValue();
        private QsVoidTypeValue() { }
    }

    // ArgTypeValues => RetValueTypes
    public class QsFuncTypeValue : QsTypeValue
    {
        public QsTypeValue Return { get; }
        public ImmutableArray<QsTypeValue> Params { get; }

        public QsFuncTypeValue(QsTypeValue ret, ImmutableArray<QsTypeValue> parameters)
        {
            Return = ret;
            Params = parameters;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncTypeValue value &&
                   EqualityComparer<QsTypeValue>.Default.Equals(Return, value.Return) &&
                   Enumerable.SequenceEqual(Params, value.Params);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Return, Params);
        }

        public static bool operator ==(QsFuncTypeValue? left, QsFuncTypeValue? right)
        {
            return EqualityComparer<QsFuncTypeValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncTypeValue? left, QsFuncTypeValue? right)
        {
            return !(left == right);
        }
    }
}
