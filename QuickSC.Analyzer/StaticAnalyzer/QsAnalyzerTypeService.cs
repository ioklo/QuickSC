using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    // analyzer inner service
    class QsAnalyzerTypeService
    {
        public bool GetTypeById(QsTypeId typeId, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsType? outType)
        {
            if (typeId.ModuleName == context.ModuleName)
                return context.TypesById.TryGetValue(typeId, out outType);
            
            foreach (var metadata in context.Metadatas)
                if (typeId.ModuleName == metadata.ModuleName)
                    return metadata.GetTypeById(typeId, out outType);

            outType = null;
            return false;
        }

        public bool GetFuncById(QsFuncId funcId, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsFunc? outFunc)
        {
            if (funcId.ModuleName == context.ModuleName)
                return context.FuncsById.TryGetValue(funcId, out outFunc);

            foreach (var metadata in context.Metadatas)
                if (funcId.ModuleName == metadata.ModuleName)
                    return metadata.GetFuncById(funcId, out outFunc);

            outFunc = null;
            return false;
        }

        public bool GetVarById(QsVarId varId, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsVariable? outVar)
        {
            if (varId.ModuleName == context.ModuleName)
                return context.VarsById.TryGetValue(varId, out outVar);

            foreach (var metadata in context.Metadatas)
                if (varId.ModuleName == metadata.ModuleName)
                    return metadata.GetVarById(varId, out outVar);

            outVar = null;
            return false;
        }

        public bool GetMemberTypeValue_NormalTypeValue(
            QsNormalTypeValue typeValue,
            string memberName,
            ImmutableArray<QsTypeValue> typeArgs,
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberTypeValue)
        {
            memberTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                return false;

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
            QsAnalyzerContext context, 
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
            QsNormalTypeValue typeValue,
            string memberName,
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            memberVarTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                return false;

            if (!type.GetMemberVarId(memberName, out var memberVar))
                return false;

            var variable = context.VarsById[memberVar.Value.VarId];

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, context, typeEnv);
            memberVarTypeValue = ApplyTypeEnv(variable.TypeValue, typeEnv);
            return true;
        }

        public bool GetMemberFunc(QsTypeId typeId, QsName name, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out (bool bStatic, QsFunc Func)? outValue)
        {
            if (!GetTypeById(typeId, context, out var type))
            {
                outValue = null;
                return false;
            }

            if (!type.GetMemberFuncId(name, out var value))
            {
                outValue = null;
                return false;
            }

            if (!GetFuncById(value.Value.FuncId, context, out var variable))
            {
                outValue = null;
                return false;
            }

            outValue = (value.Value.bStatic, variable);
            return true;
        }

        public bool GetMemberVar(QsTypeId typeId, string name, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out (bool bStatic, QsVariable Var)? outValue)
        {
            if (!GetTypeById(typeId, context, out var type))
            {
                outValue = null;
                return false;
            }

            if(!type.GetMemberVarId(name, out var value))
            {
                outValue = null;
                return false;
            }
            
            if (!GetVarById(value.Value.VarId, context, out var variable))
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
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsTypeValue? memberVarTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberVarTypeValue_NormalTypeValue(normalTypeValue, memberName, context, out memberVarTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        internal bool GetGlobalFunc(string name, int typeParamCount, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsFunc? outGlobalFunc)
        {
            // 전역 변수와는 달리 전역 함수는 다른 모듈들과 동등하게 검색한다
            var funcs = new List<QsFunc>();
            var nameElem = new QsNameElem(name, typeParamCount);

            if (context.FuncsById.TryGetValue(new QsFuncId(context.ModuleName, nameElem), out var outFunc))
                funcs.Add(outFunc);

            foreach (var refMetadata in context.Metadatas)
            {
                var funcId = new QsFuncId(refMetadata.ModuleName, nameElem);
                if (refMetadata.GetFuncById(funcId, out var outRefFunc))
                    funcs.Add(outRefFunc);
            }

            if (funcs.Count == 1)
            {
                outGlobalFunc = funcs[0];
                return true;
            }
            else if (1 < funcs.Count)
            {
                // TODO: syntaxNode를 가리키도록 해야 합니다
                context.ErrorCollector.Add(name, $"한 개 이상의 {name} 전역 함수가 있습니다");
            }
            else
            {
                context.ErrorCollector.Add(name, $"{name} 이름의 전역 함수가 없습니다");
            }

            outGlobalFunc = null;
            return false;
        }

        public bool GetGlobalVar(string name, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsVariable? outGlobalVar)
        {
            var nameElem = new QsNameElem(name, 0);

            // 내 스크립트에 있는 전역 변수가 우선한다
            if (context.VarsById.TryGetValue(new QsVarId(context.ModuleName, nameElem), out var outVar))
            {
                outGlobalVar = outVar;
                return true;
            }

            var vars = new List<QsVariable>();
            foreach (var refMetadata in context.Metadatas)
                if (refMetadata.GetVarById(new QsVarId(refMetadata.ModuleName, nameElem), out var outRefVar))
                    vars.Add(outRefVar);

            if (vars.Count == 1)
            {
                outGlobalVar = vars[0];
                return true;
            }
            else if (1 < vars.Count)
            {
                // TODO: syntaxNode를 가리키도록 해야 합니다
                context.ErrorCollector.Add(name, $"한 개 이상의 {name} 전역 변수가 있습니다");
            }
            else
            {
                context.ErrorCollector.Add(name, $"{name} 이름의 전역 함수가 없습니다");
            }

            outGlobalVar = null;
            return false;
        }

        public bool GetGlobalTypeValue(string name, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            return GetGlobalTypeValue(name, ImmutableArray<QsTypeValue>.Empty, context, out outTypeValue);
        }

        public bool GetGlobalTypeValue(
            string name, 
            ImmutableArray<QsTypeValue> typeArgs, 
            QsAnalyzerContext context, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? outTypeValue)
        {
            var nameElem = new QsNameElem(name, typeArgs.Length);
            var candidates = new List<QsTypeValue>();

            // TODO: 추후 namespace 검색도 해야 한다
            if (context.TypesById.TryGetValue(new QsTypeId(context.ModuleName, nameElem), out var globalType))
                candidates.Add(new QsNormalTypeValue(null, globalType.TypeId, typeArgs));

            foreach (var refMetadata in context.Metadatas)
            {
                if (refMetadata.GetTypeById(new QsTypeId(refMetadata.ModuleName, nameElem), out var type))
                    candidates.Add(new QsNormalTypeValue(null, type.TypeId, typeArgs));
            }

            if (candidates.Count == 1)
            {
                outTypeValue = candidates[0];
                return true;
            }
            else if (1 < candidates.Count)
            {
                outTypeValue = null;
                context.ErrorCollector.Add(name, $"이름이 같은 {name} 타입이 여러개 입니다");
                return false;
            }
            else
            {
                outTypeValue = null;
                context.ErrorCollector.Add(name, $"{name} 타입을 찾지 못했습니다");
                return false;
            }
        }

        // class X<T> { class Y<U> { S<T>.List<U> u; } } => MakeTypeValue(X<int>.Y<short>, S<T>.List<U>, context) => S<int>.Dict<short>
        public QsTypeValue MakeTypeValue(QsNormalTypeValue? outer, QsTypeValue typeValue, QsAnalyzerContext context)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            if (outer != null)
                MakeTypeEnv(outer, context, typeEnv);

            return ApplyTypeEnv(typeValue, typeEnv);
        }

        // class X<T> { class Y<U> { S<T> F<V>(V v, List<U> u); } } => MakeFuncTypeValue(X<int>.Y<short>, F, context) 
        // (V, List<short>) => S<int>
        public QsFuncTypeValue MakeFuncTypeValue(QsTypeValue? outer, QsFunc func, ImmutableArray<QsTypeValue> typeArgs, QsAnalyzerContext context)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();

            if (outer != null)
                MakeTypeEnv(outer, context, typeEnv);

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
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            funcTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                return false;

            if (!type.GetMemberFuncId(memberFuncId, out var memberFunc))
                return false;

            if (!GetFuncById(memberFunc.Value.FuncId, context, out var func))
                return false;

            if (func.TypeParams.Length != typeArgs.Length)
                return false;

            funcTypeValue = MakeFuncTypeValue(typeValue, func, typeArgs, context);
            return true;
        }

        public bool GetMemberFuncTypeValue(
            bool bStaticOnly,
            QsTypeValue typeValue,
            QsName memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs,
            QsAnalyzerContext context, 
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            // var / typeVar / normal / func
            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetMemberFuncTypeValue_NormalTypeValue(bStaticOnly, normalTypeValue, memberFuncId, typeArgs, context, out funcTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        void MakeTypeEnv_NormalTypeValue(QsNormalTypeValue typeValue, QsAnalyzerContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            if (typeValue.Outer != null)
                MakeTypeEnv(typeValue.Outer, context, typeEnv);

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                Debug.Assert(false);

            var typeParams = type.GetTypeParams();

            Debug.Assert(typeParams.Length == typeValue.TypeArgs.Length);                

            for(int i = 0; i < typeParams.Length; i++)            
                typeEnv[new QsTypeVarTypeValue(typeValue.TypeId, typeParams[i])] = typeValue.TypeArgs[i];            
        }        

        void MakeTypeEnv(QsTypeValue typeValue, QsAnalyzerContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
        {
            switch (typeValue)
            {
                case QsNormalTypeValue normalTypeValue: MakeTypeEnv_NormalTypeValue(normalTypeValue, context, typeEnv); return;
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
        bool GetBaseTypeValue_NormalTypeValue(QsNormalTypeValue typeValue, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outBaseTypeValue)
        {
            if (!GetTypeById(typeValue.TypeId, context, out var type))
            {
                outBaseTypeValue = null;
                return false;
            }

            var baseTypeValue = type.GetBaseTypeValue();
            if (baseTypeValue == null)
            {
                outBaseTypeValue = null;
                return false;
            }

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, context, typeEnv);

            outBaseTypeValue = ApplyTypeEnv(baseTypeValue, typeEnv);
            return true;
        }

        bool GetBaseTypeValue(QsTypeValue typeValue, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? baseTypeValue)
        {
            baseTypeValue = null;

            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetBaseTypeValue_NormalTypeValue(normalTypeValue, context, out baseTypeValue),
                _ => false
            };
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, QsAnalyzerContext context)
        {
            // B <- D
            // 지금은 fromType의 base들을 찾아가면서 toTypeValue와 맞는 것이 있는지 본다
            // TODO: toTypeValue가 interface라면, fromTypeValue의 interface들을 본다

            QsTypeValue? curType = fromTypeValue;
            while(true)
            {
                if (EqualityComparer<QsTypeValue>.Default.Equals(toTypeValue, curType))
                    return true;

                if (!GetBaseTypeValue(curType, context, out var outType))
                    return false;

                curType = outType;
            }
        }

        public void AddVar(QsVariable variable, QsAnalyzerContext context)
        {
            context.VarsById.Add(variable.VarId, variable);
        }

        public bool GetMemberFuncValue(bool bStaticOnly, QsTypeValue objTypeValue, QsName memberName, ImmutableArray<QsTypeValue> typeArgs, QsAnalyzerContext context, 
            [NotNullWhen(returnValue: true)] out QsFuncValue? funcValue)
        {
            funcValue = null;

            QsNormalTypeValue? ntv = objTypeValue as QsNormalTypeValue;
            if (ntv == null) return false;

            if (!GetTypeById(ntv.TypeId, context, out var type))
                return false;

            if (!type.GetMemberFuncId(memberName, out var outValue))
                return false;

            var (bStatic, memberFuncId) = outValue.Value;

            if (bStaticOnly && !bStatic) return false;

            if (!GetFuncById(memberFuncId, context, out var func))
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

        public QsFuncTypeValue GetFuncTypeValue(QsFuncValue funcValue, QsAnalyzerContext context)
        {
            if (!GetFuncById(funcValue.FuncId, context, out var func))
                throw new InvalidOperationException();

            return MakeFuncTypeValue(funcValue.Outer, func, funcValue.TypeArgs, context);
        }

        public bool GetMemberVarValue(bool bStaticOnly, QsTypeValue objTypeValue, string memberName, QsAnalyzerContext context, 
            [NotNullWhen(returnValue: true)] out QsVarValue? outVarValue)
        {
            outVarValue = null;

            var ntv = objTypeValue as QsNormalTypeValue;
            if (ntv == null) return false;

            if (!GetTypeById(ntv.TypeId, context, out var type))
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
        public QsTypeValue GetVarTypeValue(QsVarValue varValue, QsAnalyzerContext context)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            if (varValue.Outer != null)
                MakeTypeEnv(varValue.Outer, context, typeEnv);

            if (!GetVarById(varValue.VarId, context, out var variable))
                throw new InvalidOperationException();

            return ApplyTypeEnv(variable.TypeValue, typeEnv);
        }

        internal bool IsVarStatic(QsVarId varId, QsAnalyzerContext context)
        {
            return context.VarsById[varId].bStatic;
        }

        internal bool IsFuncStatic(QsFuncId funcId, QsAnalyzerContext context)
        {
            return !context.FuncsById[funcId].bThisCall;
        }
    }
}
