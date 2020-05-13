﻿using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace QuickSC
{
    // Skeleton, StaticVariable은 QsTypeInst에서 얻을 수 있게 된다
    public abstract class QsType
    {
        public QsTypeId TypeId { get; }
        public QsType(QsTypeId typeId) { TypeId = typeId; }

        public abstract string GetName();
        public abstract ImmutableArray<string> GetTypeParams();
        public abstract QsTypeValue? GetBaseTypeValue();
        public abstract bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsTypeId? typeId);
        public abstract bool GetMemberFuncId(QsMemberFuncId memberFuncId, [NotNullWhen(returnValue: true)] out QsFuncId? funcId);
        public abstract bool GetMemberVarTypeValue(string varName, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue);
    }

    //public struct QsDefaultTypeData
    //{
    //    public QsTypeValue? BaseTypeValue { get; }
    //    public ImmutableDictionary<string, QsType> MemberTypes { get; }
    //    public ImmutableDictionary<QsMemberFuncId, QsFuncType> MemberFuncTypes { get; }
    //    public ImmutableDictionary<string, QsTypeValue> MemberVarTypeValues { get; }

    //    public QsDefaultTypeData(
    //        QsTypeValue? baseTypeValue,
    //        ImmutableDictionary<string, QsType> memberTypes,
    //        ImmutableDictionary<QsMemberFuncId, QsFuncType> memberFuncTypes,
    //        ImmutableDictionary<string, QsTypeValue> memberVarTypeValues)
    //    {
    //        BaseTypeValue = baseTypeValue;
    //        MemberTypes = memberTypes;
    //        MemberFuncTypes = memberFuncTypes;
    //        MemberVarTypeValues = memberVarTypeValues;
    //    }

    //    public void Deconstruct(
    //        out QsTypeValue? baseTypeValue,
    //        out ImmutableDictionary<string, QsType> memberTypes,
    //        out ImmutableDictionary<QsMemberFuncId, QsFuncType> memberFuncTypes,
    //        out ImmutableDictionary<string, QsTypeValue> memberVarTypeValues)
    //    {
    //        baseTypeValue = BaseTypeValue;
    //        memberTypes = MemberTypes;
    //        memberFuncTypes = MemberFuncTypes;
    //        memberVarTypeValues = MemberVarTypeValues;
    //    }
    //};

    public class QsDefaultType : QsType
    {
        ImmutableArray<string> typeParams;
        string name;
        QsTypeValue? baseTypeValue;
        ImmutableDictionary<string, QsTypeId> memberTypeIds;
        ImmutableDictionary<QsMemberFuncId, QsFuncId> memberFuncIds;
        ImmutableDictionary<string, QsTypeValue> memberVarTypeValues;

        // 거의 모든 TypeValue에서 thisTypeValue를 쓰기 때문에 lazy하게 선언해야 한다
        public QsDefaultType(QsTypeId typeId,             
            string name,
            ImmutableArray<string> typeParams,
            QsTypeValue? baseTypeValue,
            ImmutableDictionary<string, QsTypeId> memberTypes,
            ImmutableDictionary<QsMemberFuncId, QsFuncId> memberFuncs,
            ImmutableDictionary<string, QsTypeValue> memberVarTypeValues)
            : base(typeId)
        {
            this.typeParams = typeParams;
            this.name = name;
            this.baseTypeValue = baseTypeValue;
            this.memberTypeIds = memberTypes;
            this.memberFuncIds = memberFuncs;
            this.memberVarTypeValues = memberVarTypeValues;
        }

        public override string GetName()
        {
            return name;
        }

        public override ImmutableArray<string> GetTypeParams()
        {
            return typeParams;
        }

        public override QsTypeValue? GetBaseTypeValue()
        {
            return baseTypeValue;
        }

        public override bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsTypeId? outTypeId)
        {
            if (memberTypeIds.TryGetValue(name, out var typeId))
            {
                outTypeId = typeId;
                return true;
            }
            else
            {
                outTypeId = null;
                return false;
            }
        }

        public override bool GetMemberFuncId(QsMemberFuncId memberFuncId, [NotNullWhen(returnValue: true)] out QsFuncId? outFuncId)
        {
            if (memberFuncIds.TryGetValue(memberFuncId, out var funcId))
            {
                outFuncId = funcId;
                return true;
            }
            else
            {
                outFuncId = null;
                return false;
            }
        }

        public override bool GetMemberVarTypeValue(string varName, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            if (memberVarTypeValues.TryGetValue(varName, out var typeValue))
            {
                outTypeValue = typeValue;
                return true;
            }
            else
            {
                outTypeValue = null;
                return false;
            }
        }
    }

    // 'Func' 객체에 대한 TypeValue가 아니라 호출가능한 값의 타입이다
    // void Func(int x) : int => void
    // 
    // class X<T>
    //     List<T> Func(); -> (void => (null, List<>, [T]))
    //     List<T> Func<U>(U u);     (U => (null, List<>, [T]))
    // Runtime 'Func'에 대한 내용이 아니라, 호출이 가능한 함수에 대한 내용이다 (lambda일수도 있고)
    //public class QsFuncType : QsType
    //{
    //    public bool bThisCall { get; } // thiscall이라면 첫번째 ArgType은 this type이다
    //    public ImmutableArray<string> TypeParams { get; }
    //    public QsTypeValue RetType { get; }
    //    public ImmutableArray<QsTypeValue> ArgTypes { get; }

    //    public QsFuncType(QsTypeId typeId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retType, ImmutableArray<QsTypeValue> argTypes)
    //        : base(typeId)
    //    {
    //        this.bThisCall = bThisCall;
    //        TypeParams = typeParams;
    //        RetType = retType;
    //        ArgTypes = argTypes;
    //    }

    //    public QsFuncType(QsTypeId typeId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retType, params QsTypeValue[] argTypes)
    //        : base(typeId)
    //    {
    //        this.bThisCall = bThisCall;
    //        TypeParams = typeParams;
    //        RetType = retType;
    //        ArgTypes = ImmutableArray.Create(argTypes);
    //    }

    //    public override ImmutableArray<string> GetTypeParams() => ImmutableArray<string>.Empty;
    //    public override QsTypeValue? GetBaseTypeValue()
    //    {
    //        // TODO: Runtime 'Func<>' 이어야 한다
    //        throw new NotImplementedException();
    //    }

    //    public override QsType? GetMemberType(string name) => null;
    //    public override QsFuncType? GetMemberFuncType(QsMemberFuncId memberFuncId) => null;
    //    public override QsTypeValue? GetMemberVarTypeValue(string name) => null;

    //}

}

