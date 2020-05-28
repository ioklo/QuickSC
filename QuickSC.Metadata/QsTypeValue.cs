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
        // public abstract ImmutableDictionary<string, QsTypeValue> MakeTypeEnv();
        // public abstract bool ApplyTypeArgs(ImmutableDictionary<string, QsTypeValue> env, [NotNullWhen(returnValue: true)] out QsTypeValue? appliedTypeValue);
        // public abstract QsTypeValue? GetMemberTypeValue(string memberName, ImmutableArray<QsTypeValue> typeArgs);
        // public abstract ImmutableArray<QsTypeValue> GetTypeArgs();
    }

    // "var"
    public class QsVarTypeValue : QsTypeValue
    {
        public static QsVarTypeValue Instance { get; } = new QsVarTypeValue();

        private QsVarTypeValue() { }

        //public override ImmutableDictionary<string, QsTypeValue> MakeTypeEnv()
        //    => throw new InvalidOperationException();

        //public override bool ApplyTypeArgs(ImmutableDictionary<string, QsTypeValue> env, [NotNullWhen(returnValue: true)] out QsTypeValue? appliedTypeValue)
        //    => throw new InvalidOperationException();

        //public override QsTypeValue? GetMemberTypeValue(string memberName, ImmutableArray<QsTypeValue> typeArgs)
        //    => throw new InvalidOperationException();

        //public override ImmutableArray<QsTypeValue> GetTypeArgs()
        //    => throw new NotImplementedException();
    }

    public abstract class QsTypeVarParent { }
    class QsTypeIdTypeVarParent : QsTypeVarParent
    {
        public QsTypeId TypeId { get; }
        public QsTypeIdTypeVarParent(QsTypeId typeId) { TypeId = typeId; }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeIdTypeVarParent parent &&
                   EqualityComparer<QsTypeId>.Default.Equals(TypeId, parent.TypeId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TypeId);
        }

        public static bool operator ==(QsTypeIdTypeVarParent? left, QsTypeIdTypeVarParent? right)
        {
            return EqualityComparer<QsTypeIdTypeVarParent?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsTypeIdTypeVarParent? left, QsTypeIdTypeVarParent? right)
        {
            return !(left == right);
        }
    }

    class QsFuncIdTypeVarParent : QsTypeVarParent
    {
        public QsFuncId FuncId { get; }
        public QsFuncIdTypeVarParent(QsFuncId funcId) { FuncId = funcId; }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncIdTypeVarParent parent &&
                   EqualityComparer<QsFuncId>.Default.Equals(FuncId, parent.FuncId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FuncId);
        }

        public static bool operator ==(QsFuncIdTypeVarParent? left, QsFuncIdTypeVarParent? right)
        {
            return EqualityComparer<QsFuncIdTypeVarParent?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncIdTypeVarParent? left, QsFuncIdTypeVarParent? right)
        {
            return !(left == right);
        }
    }

    // T
    public class QsTypeVarTypeValue : QsTypeValue
    {        
        public QsTypeVarParent Parent { get; } 
        public string Name { get; }

        public QsTypeVarTypeValue(QsFuncId funcId, string name)
        {
            Parent = new QsFuncIdTypeVarParent(funcId);
            Name = name;
        }

        public QsTypeVarTypeValue(QsTypeId typeId, string name)
        {
            Parent = new QsTypeIdTypeVarParent(typeId);
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsTypeVarTypeValue value &&
                   EqualityComparer<QsTypeVarParent>.Default.Equals(Parent, value.Parent) &&
                   Name == value.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Parent, Name);
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
        public QsTypeId TypeId { get; }
        public ImmutableArray<QsTypeValue> TypeArgs { get; }
        // public ImmutableDictionary<QsTypeVarTypeValue, QsTypeValue> TypeEnv { get; }

        public QsNormalTypeValue(QsTypeValue? outer, QsTypeId typeId, ImmutableArray<QsTypeValue> typeArgs)
        {
            this.Outer = outer;
            this.TypeId = typeId;
            this.TypeArgs = typeArgs;
            // this.typeEnv = MakeTypeEnv();
        }

        public QsNormalTypeValue(QsTypeValue? outer, QsTypeId TypeId, params QsTypeValue[] typeArgs)
        {
            this.Outer = outer;
            this.TypeId = TypeId;
            this.TypeArgs = ImmutableArray.Create(typeArgs);
            // this.typeEnv = MakeTypeEnv();
        }

        //public override ImmutableDictionary<string, QsTypeValue> MakeTypeEnv()
        //{
        //    ImmutableDictionary<string, QsTypeValue> typeEnv = (outer != null) 
        //        ? outer.MakeTypeEnv()
        //        : ImmutableDictionary<string, QsTypeValue>.Empty;

        //    var typeParams = type.GetTypeParams();
        //    for (int i = 0; i < typeParams.Length; i++)
        //        typeEnv.SetItem(typeParams[i], typeArgs[i]);

        //    return typeEnv;
        //}

        //public override bool ApplyTypeArgs(ImmutableDictionary<string, QsTypeValue> env, out QsTypeValue? appliedTypeValue)
        //{
        //    appliedTypeValue = null;

        //    QsTypeValue? appliedOuter = null;

        //    if (outer != null)
        //        if (!outer.ApplyTypeArgs(env, out appliedOuter))
        //            return false;

        //    var appliedTypeArgs = ImmutableArray.CreateBuilder<QsTypeValue>(typeArgs.Length);
        //    foreach (var typeArg in typeArgs)
        //    {
        //        if (!typeArg.ApplyTypeArgs(env, out var appliedTypeArg))
        //            return false;

        //        appliedTypeArgs.Add(appliedTypeArg);
        //    }

        //    appliedTypeValue = new QsNormalTypeValue(appliedOuter, type, appliedTypeArgs.MoveToImmutable());
        //    return true;
        //}

        //public bool GetBaseTypeValue([NotNullWhen(returnValue:true)] out QsTypeValue? typeValue)
        //{
        //    // class X<T, U> : P<T>.Base<U>
        //    // 
        //    // (X<,>, [int, short]) : ((null, P<>, int), Base<>, short)
        //    // 
        //    // (X<,>).GetBaseTypeValue() => ((null, P<>, T), Base<>, U) => 

        //    typeValue = null;

        //    // TODO: caching
        //    var baseTypeValue = type.GetBaseTypeValue();
        //    if (baseTypeValue == null)            
        //        return false;

        //    return baseTypeValue.ApplyTypeArgs(typeEnv, out typeValue);
        //}

        //public override QsTypeValue? GetMemberTypeValue(string memberName, ImmutableArray<QsTypeValue> memberArgTypes)
        //{
        //    // class X<T>
        //    // {
        //    //     class MyList<U> 
        //    //     {
        //    //     }
        //    // }
        //    // 
        //    // X<int>.MyList<short> list;
        //    // 
        //    // ((null, X<T>, [int]).GetMemberTypeValue("MyList", [short]) => 
        //    //     ((null, X<T>, [int]), MyList<U>, [short])
        //    // 
        //    // X<T>.GetMemberType("MyList") => MyList<U>
        //    // 
        //    var memberType = type.GetMemberType(memberName);
        //    if (memberType == null) return null;

        //    if (memberType.GetTypeParams().Length != memberArgTypes.Length) 
        //        return null;

        //    return new QsNormalTypeValue(this, memberType, memberArgTypes);
        //}

        //public override ImmutableArray<QsTypeValue> GetTypeArgs()
        //{
        //    return typeArgs;
        //}

        public override bool Equals(object? obj)
        {
            return obj is QsNormalTypeValue value &&
                   EqualityComparer<QsTypeValue?>.Default.Equals(Outer, value.Outer) &&
                   EqualityComparer<QsTypeId>.Default.Equals(TypeId, value.TypeId) &&
                   Enumerable.SequenceEqual(TypeArgs, value.TypeArgs);
                   // TypeEnv는 생성한 것이므로 비교하지 않아도 된다.
                   // EqualityComparer<ImmutableDictionary<string, QsTypeValue>>.Default.Equals(typeEnv, value.typeEnv);
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
    }
}
