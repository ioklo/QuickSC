using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsTypeValueServiceContext
    {
        public ImmutableDictionary<QsTypeId, QsType> TypesById { get; }
        public ImmutableDictionary<QsFuncId, QsFunc> FuncsById { get; }

        public QsTypeValueServiceContext(
            ImmutableDictionary<QsTypeId, QsType> TypesById,
            ImmutableDictionary<QsFuncId, QsFunc> funcsById)
        {
            this.TypesById = TypesById;
            this.FuncsById = funcsById;
        }
    }

    public class QsTypeValueService
    {
        public bool GetMemberTypeValue_NormalTypeValue(
            QsNormalTypeValue typeValue,
            string memberName,
            ImmutableArray<QsTypeValue> typeArgs,
            QsTypeValueServiceContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberTypeValue)
        {
            memberTypeValue = null;
            var type = context.TypesById[typeValue.TypeId];

            if (!type.GetMemberTypeId(memberName, out var memberTypeId))
                return false;            

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, context, typeEnv);
            memberTypeValue = ApplyTypeEnv(new QsNormalTypeValue(typeValue, memberTypeId.Value, typeArgs), typeEnv);
            return true;
        }

        public bool GetMemberTypeValue(
            QsTypeValue typeValue, 
            string memberName, 
            ImmutableArray<QsTypeValue> typeArgs, 
            QsTypeValueServiceContext context, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberTypeValue_NormalTypeValue(normalTypeValue, memberName, typeArgs, context, out memberTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        public bool GetMemberVarTypeValue_NormalTypeValue(
            bool bStaticOnly,
            QsNormalTypeValue typeValue,
            string memberName,
            QsTypeValueServiceContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            memberVarTypeValue = null;
            var type = context.TypesById[typeValue.TypeId];

            if (!type.GetMemberVarTypeValue(bStaticOnly, memberName, out var varTypeValue))
                return false;

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, context, typeEnv);
            memberVarTypeValue = ApplyTypeEnv(varTypeValue, typeEnv);
            return true;
        }

        public bool GetMemberVarTypeValue(
            bool bStaticOnly,
            QsTypeValue typeValue, 
            string memberName, 
            QsTypeValueServiceContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberVarTypeValue_NormalTypeValue(bStaticOnly, normalTypeValue, memberName, context, out memberVarTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        bool GetFuncTypeValue_NormalTypeValue(
            bool bStaticOnly,
            QsNormalTypeValue typeValue, 
            QsMemberFuncId memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs, 
            QsTypeValueServiceContext context,
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            funcTypeValue = null;

            var type = context.TypesById[typeValue.TypeId];

            if (!type.GetMemberFuncId(bStaticOnly, memberFuncId, out var funcId))
                return false;

            funcTypeValue = new QsFuncTypeValue(typeValue, funcId.Value, typeArgs);
            return true;
        }

        public bool GetFuncTypeValue(
            bool bStaticOnly,
            QsTypeValue typeValue, 
            QsMemberFuncId memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs,
            QsTypeValueServiceContext context, 
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetFuncTypeValue_NormalTypeValue(bStaticOnly, normalTypeValue, memberFuncId, typeArgs, context, out funcTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        void MakeTypeEnv_NormalTypeValue(QsNormalTypeValue typeValue, QsTypeValueServiceContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            if (typeValue.Outer != null)
                MakeTypeEnv(typeValue.Outer, context, typeEnv);

            var type = context.TypesById[typeValue.TypeId];
            var typeParams = type.GetTypeParams();

            Debug.Assert(typeParams.Length == typeValue.TypeArgs.Length);                

            for(int i = 0; i < typeParams.Length; i++)            
                typeEnv[new QsTypeVarTypeValue(typeValue.TypeId, typeParams[i])] = typeValue.TypeArgs[i];            
        }

        void MakeTypeEnv_FuncTypeValue(QsFuncTypeValue typeValue, QsTypeValueServiceContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            if (typeValue.Outer != null)
                MakeTypeEnv(typeValue.Outer, context, typeEnv);

            var func = context.FuncsById[typeValue.FuncId];
            var typeParams = func.TypeParams;

            Debug.Assert(typeParams.Length == typeValue.TypeArgs.Length);

            for (int i = 0; i < typeParams.Length; i++)
                typeEnv[new QsTypeVarTypeValue(typeValue.FuncId, typeParams[i])] = typeValue.TypeArgs[i];
        }

        void MakeTypeEnv(QsTypeValue typeValue, QsTypeValueServiceContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            switch (typeValue)
            {
                case QsNormalTypeValue normalTypeValue: MakeTypeEnv_NormalTypeValue(normalTypeValue, context, typeEnv); return;
                case QsFuncTypeValue funcTypeValue: MakeTypeEnv_FuncTypeValue(funcTypeValue, context, typeEnv); return;
                default: throw new NotImplementedException();
            }
        }

        QsTypeValue ApplyTypeEnv_NormalTypeValue(QsNormalTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            QsTypeValue? appliedOuter = (typeValue.Outer != null)
                ? ApplyTypeEnv(typeValue.Outer, typeEnv)
                : null;

            var appliedTypeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(typeValue.TypeArgs.Length);
            foreach(var typeArg in typeValue.TypeArgs)
            {
                var appliedTypeArg = ApplyTypeEnv(typeArg, typeEnv);
                appliedTypeArgsBuilder.Add(appliedTypeArg);
            }

            return new QsNormalTypeValue(appliedOuter, typeValue.TypeId, appliedTypeArgsBuilder.MoveToImmutable());
        }

        QsFuncTypeValue ApplyTypeEnv_FuncTypeValue(QsFuncTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            QsTypeValue? appliedOuter = (typeValue.Outer != null)
                ? ApplyTypeEnv(typeValue.Outer, typeEnv)
                : null;

            var appliedTypeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(typeValue.TypeArgs.Length);
            foreach (var typeArg in typeValue.TypeArgs)
            {
                var appliedTypeArg = ApplyTypeEnv(typeArg, typeEnv);
                appliedTypeArgsBuilder.Add(appliedTypeArg);
            }

            return new QsFuncTypeValue(appliedOuter, typeValue.FuncId, appliedTypeArgsBuilder.MoveToImmutable());
        }

        // T, [T -> ]
        QsTypeValue ApplyTypeEnv_TypeVarTypeValue(QsTypeVarTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            if (typeEnv.TryGetValue(typeValue, out var appliedTypeValue))
                return appliedTypeValue;

            return typeValue;
        }

        QsTypeValue ApplyTypeEnv(QsTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => ApplyTypeEnv_NormalTypeValue(normalTypeValue, typeEnv),
                QsFuncTypeValue funcTypeValue => ApplyTypeEnv_FuncTypeValue(funcTypeValue, typeEnv),
                QsTypeVarTypeValue typeVarTypeValue => ApplyTypeEnv_TypeVarTypeValue(typeVarTypeValue, typeEnv),
                _ => throw new NotImplementedException()
            };
        }

        public bool GetReturnTypeValue(QsFuncTypeValue funcTypeValue, QsTypeValueServiceContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            // class X<T> { class Y<U> { T Func<U>() } }
            // GetReturnTypeValue(((null, X, [int]), Y, [short]), Func, [bool])
            
            // func = T Func<U>            
            var func = context.FuncsById[funcTypeValue.FuncId];

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(funcTypeValue, context, typeEnv);
            typeValue = ApplyTypeEnv(func.RetTypeValue, typeEnv);
            return true;

            // func.RetTypeValue;
        }

        
    }
}
