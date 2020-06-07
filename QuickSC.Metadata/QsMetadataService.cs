﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC
{    
    public class QsMetadataService
    {
        string moduleName;
        ImmutableDictionary<QsTypeId, QsType> typesById;
        ImmutableDictionary<QsFuncId, QsFunc> funcsById;
        ImmutableDictionary<QsVarId, QsVariable> varsById;

        ImmutableArray<IQsMetadata> metadatas;

        public QsMetadataService(
            string moduleName,
            ImmutableDictionary<QsTypeId, QsType> typesById,
            ImmutableDictionary<QsFuncId, QsFunc> funcsById,
            ImmutableDictionary<QsVarId, QsVariable> varsById,
            ImmutableArray<IQsMetadata> metadatas)
        {
            this.moduleName = moduleName;
            this.typesById = typesById;
            this.funcsById = funcsById;
            this.varsById = varsById;

            this.metadatas = metadatas;
        }
        

        public bool GetTypeById(QsTypeId typeId, [NotNullWhen(returnValue: true)] out QsType? outType)
        {
            if (typeId.ModuleName == moduleName)
                return typesById.TryGetValue(typeId, out outType);
            
            foreach (var metadata in metadatas)
                if (typeId.ModuleName == metadata.ModuleName)
                    return metadata.GetTypeById(typeId, out outType);

            outType = null;
            return false;
        }

        public bool GetFuncById(QsFuncId funcId, [NotNullWhen(returnValue: true)] out QsFunc? outFunc)
        {
            if (funcId.ModuleName == moduleName)
                return funcsById.TryGetValue(funcId, out outFunc);

            foreach (var metadata in metadatas)
                if (funcId.ModuleName == metadata.ModuleName)
                    return metadata.GetFuncById(funcId, out outFunc);

            outFunc = null;
            return false;
        }

        public bool GetVarById(QsVarId varId, [NotNullWhen(returnValue: true)] out QsVariable? outVar)
        {
            if (varId.ModuleName == moduleName)
                return varsById.TryGetValue(varId, out outVar);

            foreach (var metadata in metadatas)
                if (varId.ModuleName == metadata.ModuleName)
                    return metadata.GetVarById(varId, out outVar);

            outVar = null;
            return false;
        }

        public bool GetMemberTypeValue_NormalTypeValue(
            QsNormalTypeValue typeValue,
            string memberName,
            ImmutableArray<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberTypeValue)
        {
            memberTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, out var type))
                return false;

            if (!type.GetMemberTypeId(memberName, out var memberTypeId))
                return false;            

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, typeEnv);
            memberTypeValue = ApplyTypeEnv(new QsNormalTypeValue(typeValue, memberTypeId.Value, typeArgs), typeEnv);
            return true;
        }

        public bool GetMemberTypeValue(
            QsTypeValue typeValue, 
            string memberName, 
            ImmutableArray<QsTypeValue> typeArgs,            
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberTypeValue_NormalTypeValue(normalTypeValue, memberName, typeArgs, out memberTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        public bool GetMemberVarTypeValue_NormalTypeValue(
            QsNormalTypeValue typeValue,
            string memberName,            
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            memberVarTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, out var type))
                return false;

            if (!type.GetMemberVarId(memberName, out var memberVar))
                return false;

            var variable = varsById[memberVar.Value.VarId];

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, typeEnv);
            memberVarTypeValue = ApplyTypeEnv(variable.TypeValue, typeEnv);
            return true;
        }

        public bool GetMemberFunc(QsTypeId typeId, QsName name, [NotNullWhen(returnValue: true)] out (bool bStatic, QsFunc Func)? outValue)
        {
            if (!GetTypeById(typeId, out var type))
            {
                outValue = null;
                return false;
            }

            if (!type.GetMemberFuncId(name, out var value))
            {
                outValue = null;
                return false;
            }

            if (!GetFuncById(value.Value.FuncId, out var variable))
            {
                outValue = null;
                return false;
            }

            outValue = (value.Value.bStatic, variable);
            return true;
        }

        public bool GetMemberVar(QsTypeId typeId, string name, [NotNullWhen(returnValue: true)] out (bool bStatic, QsVariable Var)? outValue)
        {
            if (!GetTypeById(typeId, out var type))
            {
                outValue = null;
                return false;
            }

            if(!type.GetMemberVarId(name, out var value))
            {
                outValue = null;
                return false;
            }
            
            if (!GetVarById(value.Value.VarId, out var variable))
            {
                outValue = null;
                return false;
            }

            outValue = (value.Value.bStatic, variable);
            return true;
        }

        public bool GetMemberVarTypeValue(
            QsTypeValue typeValue, 
            string memberName, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberVarTypeValue_NormalTypeValue(normalTypeValue, memberName, out memberVarTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        public bool GetGlobalFuncs(string name, int typeParamCount, out ImmutableArray<QsFunc> outGlobalFunc)
        {
            // 전역 변수와는 달리 전역 함수는 다른 모듈들과 동등하게 검색한다
            var funcsBuilder = ImmutableArray.CreateBuilder<QsFunc>();
            var nameElem = new QsNameElem(name, typeParamCount);

            if (funcsById.TryGetValue(new QsFuncId(moduleName, nameElem), out var outFunc))
                funcsBuilder.Add(outFunc);

            foreach (var refMetadata in metadatas)
            {
                var funcId = new QsFuncId(refMetadata.ModuleName, nameElem);
                if (refMetadata.GetFuncById(funcId, out var outRefFunc))
                    funcsBuilder.Add(outRefFunc);
            }

            outGlobalFunc = funcsBuilder.ToImmutable();
            return outGlobalFunc.Length != 0;
        }

        public bool GetGlobalVars(string name, out ImmutableArray<QsVariable> outGlobalVars)
        {
            var nameElem = new QsNameElem(name, 0);

            // 내 스크립트에 있는 전역 변수가 우선한다
            if (varsById.TryGetValue(new QsVarId(moduleName, nameElem), out var scriptGlobalVar))
            {
                outGlobalVars = ImmutableArray.Create(scriptGlobalVar);
                return true;
            }

            var varsBuilder = ImmutableArray.CreateBuilder<QsVariable>();
            foreach (var refMetadata in metadatas)
                if (refMetadata.GetVarById(new QsVarId(refMetadata.ModuleName, nameElem), out var refGlobalVar))
                    varsBuilder.Add(refGlobalVar);

            outGlobalVars = varsBuilder.ToImmutable();
            return outGlobalVars.Length != 0;
        }

        public bool GetGlobalTypeValue(string name, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            if (GetGlobalTypeValues(name, ImmutableArray<QsTypeValue>.Empty, out var typeValues) && typeValues.Length == 1)
            {
                outTypeValue = typeValues[0];
                return true;
            }
            else
            {
                outTypeValue = null;
                return false;
            }
        }

        public bool GetGlobalTypeValues(
            string name, 
            ImmutableArray<QsTypeValue> typeArgs,             
            out ImmutableArray<QsTypeValue> outTypeValues)
        {
            var nameElem = new QsNameElem(name, typeArgs.Length);
            var typeValuesBuilder = ImmutableArray.CreateBuilder<QsTypeValue>();

            // TODO: 추후 namespace 검색도 해야 한다
            if (typesById.TryGetValue(new QsTypeId(moduleName, nameElem), out var globalType))
                typeValuesBuilder.Add(new QsNormalTypeValue(null, globalType.TypeId, typeArgs));

            foreach (var refMetadata in metadatas)
            {
                if (refMetadata.GetTypeById(new QsTypeId(refMetadata.ModuleName, nameElem), out var type))
                    typeValuesBuilder.Add(new QsNormalTypeValue(null, type.TypeId, typeArgs));
            }

            outTypeValues = typeValuesBuilder.ToImmutableArray();
            return outTypeValues.Length != 0;
        }

        // class X<T> { class Y<U> { S<T>.List<U> u; } } => MakeTypeValue(X<int>.Y<short>, S<T>.List<U>, context) => S<int>.Dict<short>
        public QsTypeValue MakeTypeValue(QsNormalTypeValue? outer, QsTypeValue typeValue)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            if (outer != null)
                MakeTypeEnv(outer, typeEnv);

            return ApplyTypeEnv(typeValue, typeEnv);
        }

        // class X<T> { class Y<U> { S<T> F<V>(V v, List<U> u); } } => MakeFuncTypeValue(X<int>.Y<short>, F, context) 
        // (V, List<short>) => S<int>
        public QsFuncTypeValue MakeFuncTypeValue(QsTypeValue? outer, QsFunc func, ImmutableArray<QsTypeValue> typeArgs)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();

            if (outer != null)
                MakeTypeEnv(outer, typeEnv);

            for (int i = 0; i < func.TypeParams.Length; i++)
                typeEnv[new QsTypeVarTypeValue(func.FuncId, func.TypeParams[i])] = typeArgs[i];

            return ApplyTypeEnv_FuncTypeValue(new QsFuncTypeValue(func.RetTypeValue, func.ParamTypeValues), typeEnv);
        }

        // 
        // GetFuncTypeValue_NormalTypeValue(X<int>.Y<short>, "Func", <bool>) =>   (int, short) => bool
        // 
        bool GetMemberFuncTypeValue_NormalTypeValue(
            bool bStaticOnly,
            QsNormalTypeValue typeValue,
            QsName memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs, 
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            funcTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, out var type))
                return false;

            if (!type.GetMemberFuncId(memberFuncId, out var memberFunc))
                return false;

            if (!GetFuncById(memberFunc.Value.FuncId, out var func))
                return false;

            if (func.TypeParams.Length != typeArgs.Length)
                return false;

            funcTypeValue = MakeFuncTypeValue(typeValue, func, typeArgs);
            return true;
        }

        public bool GetMemberFuncTypeValue(
            bool bStaticOnly,
            QsTypeValue typeValue,
            QsName memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs,
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberFuncTypeValue_NormalTypeValue(bStaticOnly, normalTypeValue, memberFuncId, typeArgs, out funcTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        public void MakeTypeEnv_NormalTypeValue(QsNormalTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            if (typeValue.Outer != null)
                MakeTypeEnv(typeValue.Outer, typeEnv);

            if (!GetTypeById(typeValue.TypeId, out var type))
                Debug.Assert(false);

            var typeParams = type.GetTypeParams();

            Debug.Assert(typeParams.Length == typeValue.TypeArgs.Length);                

            for(int i = 0; i < typeParams.Length; i++)            
                typeEnv[new QsTypeVarTypeValue(typeValue.TypeId, typeParams[i])] = typeValue.TypeArgs[i];            
        }        

        public void MakeTypeEnv(QsTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            switch (typeValue)
            {
                case QsNormalTypeValue normalTypeValue: MakeTypeEnv_NormalTypeValue(normalTypeValue, typeEnv); return;
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


        // 
        QsFuncTypeValue ApplyTypeEnv_FuncTypeValue(QsFuncTypeValue typeValue, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            return new QsFuncTypeValue(
                ApplyTypeEnv(typeValue.Return, typeEnv),
                ImmutableArray.CreateRange(
                    typeValue.Params,
                    paramTypeValue => ApplyTypeEnv(paramTypeValue, typeEnv)));
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
                QsVoidTypeValue vtv => vtv,
                _ => throw new NotImplementedException()
            };
        }

        // class N<T> : B<T> => N.GetBaseType => B<T(N)>
        public bool GetBaseTypeValue_NormalTypeValue(QsNormalTypeValue typeValue, out QsTypeValue? outBaseTypeValue)
        {
            if (!GetTypeById(typeValue.TypeId, out var type))
            {
                outBaseTypeValue = null;
                return false;
            }

            var baseTypeValue = type.GetBaseTypeValue();
            if (baseTypeValue == null)
            {
                outBaseTypeValue = null;
                return true; // BaseType은 null일 수 있다
            }

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, typeEnv);

            outBaseTypeValue = ApplyTypeEnv(baseTypeValue, typeEnv);
            return true;
        }

        public bool GetBaseTypeValue(QsTypeValue typeValue, out QsTypeValue? baseTypeValue)
        {
            baseTypeValue = null;

            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetBaseTypeValue_NormalTypeValue(normalTypeValue, out baseTypeValue),
                _ => false
            };
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue)
        {
            // B <- D
            // 지금은 fromType의 base들을 찾아가면서 toTypeValue와 맞는 것이 있는지 본다
            // TODO: toTypeValue가 interface라면, fromTypeValue의 interface들을 본다

            QsTypeValue? curType = fromTypeValue;
            while(curType != null)
            {
                if (EqualityComparer<QsTypeValue>.Default.Equals(toTypeValue, curType))
                    return true;

                if (!GetBaseTypeValue(curType, out var outType))
                    return false;

                curType = outType;
            }

            return false;
        }

        public void AddVar(QsVariable variable)
        {
            varsById = varsById.Add(variable.VarId, variable);
        }

        public bool GetMemberFuncValue(bool bStaticOnly, QsTypeValue objTypeValue, QsName memberName, ImmutableArray<QsTypeValue> typeArgs, 
            [NotNullWhen(returnValue: true)] out QsFuncValue? funcValue)
        {
            funcValue = null;

            QsNormalTypeValue? ntv = objTypeValue as QsNormalTypeValue;
            if (ntv == null) return false;

            if (!GetTypeById(ntv.TypeId, out var type))
                return false;

            if (!type.GetMemberFuncId(memberName, out var outValue))
                return false;

            var (bStatic, memberFuncId) = outValue.Value;

            if (bStaticOnly && !bStatic) return false;

            if (!GetFuncById(memberFuncId, out var func))
                return false;

            // 함수는 typeArgs가 모자라도 최대한 매칭한다
            if (func.TypeParams.Length < typeArgs.Length)
                return false;

            var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(func.TypeParams.Length);

            foreach(var typeArg in typeArgs)
                typeArgsBuilder.Add(typeArg);

            foreach(var typeParam in func.TypeParams)
                typeArgsBuilder.Add(new QsTypeVarTypeValue(func.FuncId, typeParam));

            funcValue = new QsFuncValue(objTypeValue, func.FuncId, typeArgsBuilder.MoveToImmutable());
            return true;
        }

        public QsFuncTypeValue GetFuncTypeValue(QsFuncValue funcValue)
        {
            if (!GetFuncById(funcValue.FuncId, out var func))
                throw new InvalidOperationException();

            return MakeFuncTypeValue(funcValue.Outer, func, funcValue.TypeArgs);
        }

        public bool GetMemberVarValue(bool bStaticOnly, QsTypeValue objTypeValue, string memberName,
            [NotNullWhen(returnValue: true)] out QsVarValue? outVarValue)
        {
            outVarValue = null;

            var ntv = objTypeValue as QsNormalTypeValue;
            if (ntv == null) return false;

            if (!GetTypeById(ntv.TypeId, out var type))
                return false;
            
            if (!type.GetMemberVarId(memberName, out var outValue))
                return false;

            var (bStatic, varId) = outValue.Value;

            if (bStaticOnly && !bStatic) return false;

            outVarValue = new QsVarValue(objTypeValue, varId);
            return true;
        }

        // X<T>.Y<U>{ Dict<T, U> x; } 
        // GetVarTypeValue(X<int>.Y<short>, x) => Dict<int, short>
        public QsTypeValue GetVarTypeValue(QsVarValue varValue)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            if (varValue.Outer != null)
                MakeTypeEnv(varValue.Outer, typeEnv);

            if (!GetVarById(varValue.VarId, out var variable))
                throw new InvalidOperationException();

            return ApplyTypeEnv(variable.TypeValue, typeEnv);
        }

        public bool IsVarStatic(QsVarId varId)
        {
            if (!GetVarById(varId, out var variable))
                throw new InvalidOperationException();

            return variable.bStatic;
        }

        public bool IsFuncStatic(QsFuncId funcId)
        {
            if (!GetFuncById(funcId, out var func))
                throw new InvalidOperationException();

            return !func.bThisCall;
        }
    }
}
