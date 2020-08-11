using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace QuickSC
{
    // Skeleton, StaticVariable은 QsTypeInst에서 얻을 수 있게 된다
    public abstract class QsTypeInfo
    {
        public QsMetaItemId TypeId { get; }
        
        public QsTypeInfo(QsMetaItemId typeId) 
        { 
            TypeId = typeId; 
        }

        public abstract QsMetaItemId? GetOuterTypeId();
        
        public abstract ImmutableArray<string> GetTypeParams();
        public abstract QsTypeValue? GetBaseTypeValue();

        // TODO: 셋은 같은 이름공간을 공유한다. 서로 이름이 같은 것이 나오면 안된다 (체크하자)
        public abstract bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsMetaItemId? outTypeId);
        public abstract bool GetMemberFuncId(QsName memberFuncId, [NotNullWhen(returnValue: true)] out QsMetaItemId? outFuncId);
        public abstract bool GetMemberVarId(QsName name, [NotNullWhen(returnValue: true)] out QsMetaItemId? outVarId);
    }

    public class QsDefaultTypeInfo : QsTypeInfo
    {
        QsMetaItemId? outerTypeId;

        ImmutableArray<string> typeParams;
        QsTypeValue? baseTypeValue;
        ImmutableDictionary<QsName, QsMetaItemId> memberTypeIds;
        ImmutableDictionary<QsName, QsMetaItemId> memberFuncIds;
        ImmutableDictionary<QsName, QsMetaItemId> memberVarIds;        

        // 거의 모든 TypeValue에서 thisTypeValue를 쓰기 때문에 lazy하게 선언해야 한다
        public QsDefaultTypeInfo(
            QsMetaItemId? outerTypeId,
            QsMetaItemId typeId,            
            IEnumerable<string> typeParams,
            QsTypeValue? baseTypeValue,
            IEnumerable<QsMetaItemId> memberTypeIds,
            IEnumerable<QsMetaItemId> memberFuncIds,
            IEnumerable<QsMetaItemId> memberVarIds)
            : base(typeId)
        {
            this.outerTypeId = outerTypeId;
            this.typeParams = typeParams.ToImmutableArray();
            this.baseTypeValue = baseTypeValue;
            this.memberTypeIds = memberTypeIds.ToImmutableDictionary(memberTypeId => memberTypeId.Name);
            this.memberFuncIds = memberFuncIds.ToImmutableDictionary(memberFuncId => memberFuncId.Name);
            this.memberVarIds = memberVarIds.ToImmutableDictionary(memberVarId => memberVarId.Name);
        }

        public override QsMetaItemId? GetOuterTypeId()
        {
            return outerTypeId;
        }

        public override ImmutableArray<string> GetTypeParams()
        {
            return typeParams;
        }

        public override QsTypeValue? GetBaseTypeValue()
        {
            return baseTypeValue;
        }

        public override bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsMetaItemId? outTypeId)
        {
            if (memberTypeIds.TryGetValue(QsName.MakeText(name), out var typeId))
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

        public override bool GetMemberFuncId(QsName memberFuncName, [NotNullWhen(returnValue: true)] out QsMetaItemId? outFuncId)
        {
            // TODO: 같은 이름 체크?
            if (memberFuncIds.TryGetValue(memberFuncName, out var funcId))
            {
                outFuncId = funcId;
                return true;
            }

            outFuncId = null;
            return false;
        }

        public override bool GetMemberVarId(QsName varName, [NotNullWhen(returnValue: true)] out QsMetaItemId? outVarId)
        {
            // TODO: 같은 이름 체크
            if (memberVarIds.TryGetValue(varName, out var varId))
            {
                outVarId = varId;
                return true;
            }

            outVarId = null;
            return false;
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

    //    public QsFuncType(QsMetaItemId typeId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retType, ImmutableArray<QsTypeValue> argTypes)
    //        : base(typeId)
    //    {
    //        this.bThisCall = bThisCall;
    //        TypeParams = typeParams;
    //        RetType = retType;
    //        ArgTypes = argTypes;
    //    }

    //    public QsFuncType(QsMetaItemId typeId, bool bThisCall, ImmutableArray<string> typeParams, QsTypeValue retType, params QsTypeValue[] argTypes)
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

