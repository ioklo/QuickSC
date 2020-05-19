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
    public abstract class QsType
    {
        public QsTypeId TypeId { get; }
        public QsType(QsTypeId typeId) { TypeId = typeId; }

        public abstract string GetName();
        public abstract ImmutableArray<string> GetTypeParams();
        public abstract QsTypeValue? GetBaseTypeValue();

        // TODO: 셋은 같은 이름공간을 공유한다. 서로 이름이 같은 것이 나오면 안된다 (체크하자)
        public abstract bool GetMemberTypeId(string name, [NotNullWhen(returnValue: true)] out QsTypeId? outTypeId);
        public abstract bool GetMemberFuncId(QsMemberFuncId memberFuncId, [NotNullWhen(returnValue: true)] out (bool bStatic, QsFuncId FuncId)? outValue);
        public abstract bool GetMemberVarId(string varName, [NotNullWhen(returnValue: true)] out (bool bStatic, QsVarId VarId)? outValue);
    }

    public class QsDefaultType : QsType
    {
        ImmutableArray<string> typeParams;
        string name;
        QsTypeValue? baseTypeValue;
        ImmutableDictionary<string, QsTypeId> memberTypeIds;
        ImmutableDictionary<string, QsFuncId> staticMemberFuncIds;
        ImmutableDictionary<string, QsVarId> staticMemberVarIds;

        ImmutableDictionary<QsMemberFuncId, QsFuncId> memberFuncIds;
        ImmutableDictionary<string, QsVarId> memberVarIds;        

        // 거의 모든 TypeValue에서 thisTypeValue를 쓰기 때문에 lazy하게 선언해야 한다
        public QsDefaultType(QsTypeId typeId,             
            string name,
            ImmutableArray<string> typeParams,
            QsTypeValue? baseTypeValue,
            ImmutableDictionary<string, QsTypeId> memberTypes,
            ImmutableDictionary<string, QsFuncId> staticMemberFuncIds,
            ImmutableDictionary<string, QsVarId> staticMemberVarIds,
            ImmutableDictionary<QsMemberFuncId, QsFuncId> memberFuncs,
            ImmutableDictionary<string, QsVarId> memberVarIds)
            : base(typeId)
        {
            this.typeParams = typeParams;
            this.name = name;
            this.baseTypeValue = baseTypeValue;
            this.memberTypeIds = memberTypes;
            this.staticMemberFuncIds = staticMemberFuncIds;
            this.staticMemberVarIds = staticMemberVarIds;

            this.memberFuncIds = memberFuncs;
            this.memberVarIds = memberVarIds;
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

        public override bool GetMemberFuncId(QsMemberFuncId memberFuncId, [NotNullWhen(returnValue: true)] out (bool bStatic, QsFuncId FuncId)? outValue)
        {   
            // TODO: 같은 이름 체크?
            if (memberFuncIds.TryGetValue(memberFuncId, out var funcId))
            {
                outValue = (false, funcId);
                return true;
            }

            if (!string.IsNullOrEmpty(memberFuncId.Name) && staticMemberFuncIds.TryGetValue(memberFuncId.Name, out funcId))
            {
                outValue = (true, funcId);
                return true;
            }            
            
            outValue = null;
            return false;
        }

        public override bool GetMemberVarId(string varName, [NotNullWhen(returnValue: true)] out (bool bStatic, QsVarId VarId)? outValue)
        {
            // TODO: 같은 이름 체크
            if (memberVarIds.TryGetValue(varName, out var varId))
            {
                outValue = (false, varId);
                return true;
            }

            if (staticMemberVarIds.TryGetValue(varName, out varId))
            {
                outValue = (true, varId);
                return true;
            }

            outValue = null;
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

