using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace QuickSC
{
#pragma warning disable CS0660, CS0661
    public abstract class QsTypeValue
    {
        // "var"
        public class Var : QsTypeValue
        {
            public static Var Instance { get; } = new Var();
            private Var() { }
        }

        // T
        public class TypeVar : QsTypeValue
        {
            public QsMetaItemId ParentId { get; }
            public string Name { get; }

            internal TypeVar(QsMetaItemId parentId, string name)
            {
                ParentId = parentId;
                Name = name;
            }

            public override bool Equals(object? obj)
            {
                return obj is TypeVar value &&
                       EqualityComparer<QsMetaItemId>.Default.Equals(ParentId, value.ParentId) &&
                       Name == value.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ParentId, Name);
            }
        }
        
        public class Normal : QsTypeValue
        {
            public QsMetaItemId TypeId { get; }
            public QsTypeArgumentList TypeArgList { get; }

            internal Normal(QsMetaItemId typeId, QsTypeArgumentList typeArgList)
            {
                this.TypeId = typeId;
                this.TypeArgList = typeArgList;
            }

            public override bool Equals(object? obj)
            {
                return obj is Normal value &&
                       EqualityComparer<QsMetaItemId>.Default.Equals(TypeId, value.TypeId) &&
                       EqualityComparer<QsTypeArgumentList>.Default.Equals(TypeArgList, value.TypeArgList);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(TypeId, TypeArgList);
            }
        }

        // "void"
        public class Void : QsTypeValue
        {
            public static Void Instance { get; } = new Void();
            private Void() { }
        }

        // ArgTypeValues => RetValueTypes
        public class Func : QsTypeValue
        {
            public QsTypeValue Return { get; }
            public ImmutableArray<QsTypeValue> Params { get; }

            public Func(QsTypeValue ret, IEnumerable<QsTypeValue> parameters)
            {
                Return = ret;
                Params = parameters.ToImmutableArray();
            }

            public override bool Equals(object? obj)
            {
                return obj is Func value &&
                       EqualityComparer<QsTypeValue>.Default.Equals(Return, value.Return) &&
                       Enumerable.SequenceEqual(Params, value.Params);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Return, Params);
            }
        }

        public class EnumElem : QsTypeValue
        {
            public Normal EnumTypeValue { get; }
            public string Name { get; }

            public EnumElem(Normal enumTypeValue, string name)
            {
                EnumTypeValue = enumTypeValue;
                Name = name;
            }
        }

        public static Var MakeVar() => Var.Instance;
        public static TypeVar MakeTypeVar(QsMetaItemId parentId, string name) => new TypeVar(parentId, name);
        public static Normal MakeNormal(QsMetaItemId typeId, QsTypeArgumentList args) => new Normal(typeId, args);
        public static Normal MakeNormal(QsMetaItemId typeId) => new Normal(typeId, QsTypeArgumentList.Empty);
        public static Void MakeVoid() => Void.Instance;
        public static Func MakeFunc(QsTypeValue ret, IEnumerable<QsTypeValue> parameters) => new Func(ret, parameters);
        public static EnumElem MakeEnumElem(Normal enumTypeValue, string name) => new EnumElem(enumTypeValue, name);

        // opeator
        public static bool operator ==(QsTypeValue? left, QsTypeValue? right)
        {
            return EqualityComparer<QsTypeValue?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeValue? left, QsTypeValue? right)
        {
            return !(left == right);
        }

        
    }
    
#pragma warning restore CS0660, CS0661
}
