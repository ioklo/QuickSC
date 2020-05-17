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
    public class QsTypeValueServiceContext
    {
        public ImmutableArray<IQsMetadata> RefMetadatas { get; }
        public ImmutableDictionary<string, QsType> GlobalTypes { get; }
        public ImmutableDictionary<string, QsFunc> GlobalFuncs { get; }
        public ImmutableDictionary<QsTypeId, QsType> TypesById { get; }
        public ImmutableDictionary<QsFuncId, QsFunc> FuncsById { get; }
        public ImmutableDictionary<QsVarId, QsVariable> VarsById { get; }
        public List<(object Obj, string Message)> Errors { get; }

        public Dictionary<string, QsVariable> GlobalVars { get; }

        public QsTypeValueServiceContext(
            ImmutableArray<IQsMetadata> refMetadatas,
            ImmutableDictionary<string, QsType> globalTypes,
            ImmutableDictionary<string, QsFunc> globalFuncs,
            ImmutableDictionary<QsTypeId, QsType> typesById,
            ImmutableDictionary<QsFuncId, QsFunc> funcsById,
            ImmutableDictionary<QsVarId, QsVariable> varsById,
            List<(object Obj, string Message)> errors)
        {
            RefMetadatas = refMetadatas;
            GlobalTypes = globalTypes;
            GlobalFuncs = globalFuncs;
            TypesById = typesById;
            FuncsById = funcsById;
            VarsById = varsById;
            Errors = errors;
            GlobalVars = new Dictionary<string, QsVariable>();
        }
    }

    public class QsTypeValueService
    {
        QsVarIdFactory varIdFactory;

        public QsTypeValueService(QsVarIdFactory varIdFactory)
        {
            this.varIdFactory = varIdFactory;
        }

        public bool GetTypeById(QsTypeId typeId, QsTypeValueServiceContext context, [NotNullWhen(returnValue: true)] out QsType? type)
        {
            if (typeId.Metadata != null)
                return typeId.Metadata.GetTypeById(typeId, out type);

            return context.TypesById.TryGetValue(typeId, out type);
        }

        public bool GetFuncById(QsFuncId funcId, QsTypeValueServiceContext context, [NotNullWhen(returnValue: true)] out QsFunc? func)
        {
            if (funcId.Metadata != null)
                return funcId.Metadata.GetFuncById(funcId, out func);

            return context.FuncsById.TryGetValue(funcId, out func);
        }
        
        public bool GetMemberTypeValue_NormalTypeValue(
            QsNormalTypeValue typeValue,
            string memberName,
            ImmutableArray<QsTypeValue> typeArgs,
            QsTypeValueServiceContext context,
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

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                return false;

            if (!type.GetMemberVarId(bStaticOnly, memberName, out var varId))
                return false;

            var variable = context.VarsById[varId.Value];

            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();
            MakeTypeEnv(typeValue, context, typeEnv);
            memberVarTypeValue = ApplyTypeEnv(variable.TypeValue, typeEnv);
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

        internal bool GetGlobalFunc(string name, QsTypeValueServiceContext context, out QsFunc? outGlobalFunc)
        {
            var funcs = new List<QsFunc>();

            if (context.GlobalFuncs.TryGetValue(name, out var outFunc))
                funcs.Add(outFunc);

            foreach (var refMetadata in context.RefMetadatas)
                if (refMetadata.GetGlobalFunc(name, out var outRefFunc))
                    funcs.Add(outRefFunc);

            if (funcs.Count == 1)
            {
                outGlobalFunc = funcs[0];
                return true;
            }
            else if (1 < funcs.Count)
            {
                // TODO: syntaxNode를 가리키도록 해야 합니다
                context.Errors.Add((name, $"한 개 이상의 {name} 전역 함수가 있습니다"));
            }
            else
            {
                context.Errors.Add((name, $"{name} 이름의 전역 함수가 없습니다"));
            }

            outGlobalFunc = null;
            return false;
        }

        public bool GetGlobalVar(string name, QsTypeValueServiceContext context, out QsVariable? outGlobalVar)
        {
            var vars = new List<QsVariable>();

            if (context.GlobalVars.TryGetValue(name, out var outVar))
                vars.Add(outVar);

            foreach (var refMetadata in context.RefMetadatas)
                if (refMetadata.GetGlobalVar(name, out var outRefVar))
                    vars.Add(outRefVar);

            if (vars.Count == 1)
            {
                outGlobalVar = vars[0];
                return true;
            }
            else if (1 < vars.Count)
            {
                // TODO: syntaxNode를 가리키도록 해야 합니다
                context.Errors.Add((name, $"한 개 이상의 {name} 전역 변수가 있습니다"));
            }
            else
            {
                context.Errors.Add((name, $"{name} 이름의 전역 함수가 없습니다"));
            }

            outGlobalVar = null;
            return false;
        }

        public bool GetGlobalTypeValue(
            string name, 
            ImmutableArray<QsTypeValue> typeArgs, 
            QsTypeValueServiceContext context, 
            [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            var candidates = new List<QsTypeValue>();

            // TODO: 추후 namespace 검색도 해야 한다
            if (context.GlobalTypes.TryGetValue(name, out var globalType))
                candidates.Add(new QsNormalTypeValue(null, globalType.TypeId, typeArgs));

            foreach (var refMetadata in context.RefMetadatas)
            {
                if (refMetadata.GetGlobalType(name, typeArgs.Length, out var type))
                    candidates.Add(new QsNormalTypeValue(null, type.TypeId, typeArgs));
            }

            if (candidates.Count == 1)
            {
                typeValue = candidates[0];
                return true;
            }
            else if (1 < candidates.Count)
            {
                typeValue = null;
                context.Errors.Add((name, $"이름이 같은 {name} 타입이 여러개 입니다"));
                return false;
            }
            else
            {
                typeValue = null;
                context.Errors.Add((name, $"{name} 타입을 찾지 못했습니다"));
                return false;
            }
        }

        public QsFuncTypeValue MakeFuncTypeValue(QsNormalTypeValue? outer, QsFunc func, ImmutableArray<QsTypeValue> typeArgs, QsTypeValueServiceContext context)
        {
            var typeEnv = new Dictionary<QsTypeVarTypeValue, QsTypeValue>();

            if (outer != null)
                MakeTypeEnv(outer, context, typeEnv);

            for (int i = 0; i < func.TypeParams.Length; i++)
                typeEnv[new QsTypeVarTypeValue(func.FuncId, func.TypeParams[i])] = typeArgs[i];

            return new QsFuncTypeValue(
                ApplyTypeEnv(func.RetTypeValue, typeEnv),
                ImmutableArray.CreateRange(func.ParamTypeValues, paramType => ApplyTypeEnv(paramType, typeEnv)));
        }

        // 
        // GetFuncTypeValue_NormalTypeValue(X<int>.Y<short>, "Func", <bool>) =>   (int, short) => bool
        // 
        bool GetMemberFuncTypeValue_NormalTypeValue(
            bool bStaticOnly,
            QsNormalTypeValue typeValue,
            QsMemberFuncId memberFuncId, 
            ImmutableArray<QsTypeValue> typeArgs, 
            QsTypeValueServiceContext context,
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? funcTypeValue)
        {
            funcTypeValue = null;

            if (!GetTypeById(typeValue.TypeId, context, out var type))
                return false;

            if (!type.GetMemberFuncId(bStaticOnly, memberFuncId, out var funcId))
                return false;

            if (!GetFuncById(funcId.Value, context, out var func))
                return false;

            if (func.TypeParams.Length != typeArgs.Length)
                return false;

            funcTypeValue = MakeFuncTypeValue(typeValue, func, typeArgs, context);
            return true;
        }

        public bool GetMemberFuncTypeValue(
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
                QsNormalTypeValue normalTypeValue => GetMemberFuncTypeValue_NormalTypeValue(bStaticOnly, normalTypeValue, memberFuncId, typeArgs, context, out funcTypeValue),
                _ => throw new NotImplementedException()
            };
        }

        void MakeTypeEnv_NormalTypeValue(QsNormalTypeValue typeValue, QsTypeValueServiceContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
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

        void MakeTypeEnv(QsTypeValue typeValue, QsTypeValueServiceContext context, Dictionary<QsTypeVarTypeValue, QsTypeValue> typeEnv)
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
                ApplyTypeEnv(typeValue.RetTypeValue, typeEnv),
                ImmutableArray.CreateRange(
                    typeValue.ParamTypeValues,
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
                _ => throw new NotImplementedException()
            };
        }

        // class N<T> : B<T> => N.GetBaseType => B<T(N)>
        bool GetBaseTypeValue_NormalTypeValue(QsNormalTypeValue typeValue, QsTypeValueServiceContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? outBaseTypeValue)
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

        bool GetBaseTypeValue(QsTypeValue typeValue, QsTypeValueServiceContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? baseTypeValue)
        {
            baseTypeValue = null;

            return typeValue switch
            {
                QsNormalTypeValue normalTypeValue => GetBaseTypeValue_NormalTypeValue(normalTypeValue, context, out baseTypeValue),
                _ => false
            };
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, QsTypeValueServiceContext context)
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

        public void AddGlobalVar(string varName, QsTypeValue typeValue, QsTypeValueServiceContext context)
        {
            var varId = varIdFactory.MakeVarId();
            var globalVar = new QsVariable(varId, typeValue, varName);

            context.GlobalVars.Add(varName, globalVar);
        }
    }
}
