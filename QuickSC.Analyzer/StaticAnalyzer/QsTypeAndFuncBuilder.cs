using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace QuickSC.StaticAnalyzer
{
    class QsTypeAndFuncBuilderContext
    {
        public QsTypeEvalInfo TypeEvalInfo { get; }

        public QsTypeBuilder? TypeBuilder { get; set; }
        public List<QsType> Types { get;} // All Types
        public List<QsFunc> Funcs { get; } // All Funcs
        public List<QsVariable> Vars { get; } // Type의 Variable

        public List<QsFunc> GlobalFuncs { get; } // GlobalFuncs
        public List<QsType> GlobalTypes { get; }

        public QsTypeAndFuncBuilderContext(QsTypeEvalInfo typeEvalInfo)
        {
            TypeEvalInfo = typeEvalInfo;
            
            TypeBuilder = null;
            Types = new List<QsType>();
            Funcs = new List<QsFunc>();
            Vars = new List<QsVariable>();
            GlobalFuncs = new List<QsFunc>();
            GlobalTypes = new List<QsType>();
        }
    }

    public class QsTypeBuildInfo
    {
        public ImmutableDictionary<QsTypeId, QsType> TypesById { get; }
        public ImmutableDictionary<QsFuncId, QsFunc> FuncsById { get; }
        public Dictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public Dictionary<QsVarId, QsVariable> VarsById { get; }

        public QsTypeBuildInfo(
            ImmutableDictionary<QsTypeId, QsType> typesById, 
            ImmutableDictionary<QsFuncId, QsFunc> funcsById,
            Dictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp,
            Dictionary<QsVarId, QsVariable> varsById)
        {
            TypesById = typesById;
            FuncsById = funcsById;
            TypeValuesByTypeExp = typeValuesByTypeExp;
            VarsById = varsById;
        }
    }

    class QsTypeBuilder
    {
        public QsTypeValue ThisTypeValue { get; }
        public Dictionary<string, QsTypeId> MemberTypeIds { get; }
        public Dictionary<string, QsFuncId> StaticMemberFuncIds { get; }
        public Dictionary<string, QsVarId> StaticMemberVarIds { get; }
        public Dictionary<QsName, QsFuncId> MemberFuncIds { get; }
        public Dictionary<string, QsVarId> MemberVarIds { get; }

        public QsTypeBuilder(QsTypeValue thisTypeValue)
        {
            ThisTypeValue = thisTypeValue;

            MemberTypeIds = new Dictionary<string, QsTypeId>();
            StaticMemberFuncIds = new Dictionary<string, QsFuncId>();
            StaticMemberVarIds = new Dictionary<string, QsVarId>();
            MemberFuncIds = new Dictionary<QsName, QsFuncId>();
            MemberVarIds = new Dictionary<string, QsVarId>();
        }
    }

    // TODO: 이름을 TypeAndFuncBuilder로..
    public class QsTypeAndFuncBuilder
    {
        public QsTypeAndFuncBuilder()
        {   
        }
        
        void BuildEnumDeclElement(QsEnumDeclElement elem, QsTypeAndFuncBuilderContext context)
        {
            var thisTypeValue = context.TypeBuilder!.ThisTypeValue;

            // 타입 추가
            var elemType = new QsDefaultType(
                context.TypeEvalInfo.TypeIdsByLocation[QsTypeIdLocation.Make(elem)], 
                ImmutableArray<string>.Empty,                
                thisTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<string, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty,
                ImmutableDictionary<QsName, QsFuncId>.Empty,
                ImmutableDictionary<string, QsVarId>.Empty);

            context.TypeBuilder.MemberTypeIds.Add(elem.Name, elemType.TypeId);

            if (0 < elem.Params.Length)
            {
                var argTypes = ImmutableArray.CreateBuilder<QsTypeValue>(elem.Params.Length);
                foreach (var param in elem.Params)
                {
                    var typeValue = context.TypeEvalInfo.TypeValuesByTypeExp[param.Type];
                    argTypes.Add(typeValue);
                }

                // Func를 만들까 FuncSkeleton을 만들까
                var func = MakeFunc(
                    context.TypeEvalInfo.FuncIdsByLocation[QsFuncIdLocation.Make(elem)], 
                    false, 
                    ImmutableArray<string>.Empty, 
                    thisTypeValue, 
                    argTypes.MoveToImmutable(), 
                    context);

                context.TypeBuilder.StaticMemberFuncIds.Add(elem.Name, func.FuncId);
            }
            else
            {
                // NOTICE : E.First 타입이 아니라 E 타입이다 var x = E.First; 에서 x는 E여야 하기 떄문
                var variable = MakeVar(
                    new QsVarId(null, new QsNameElem(elem.Name, 0)),
                    thisTypeValue,
                    context);

                context.TypeBuilder.StaticMemberVarIds.Add(elem.Name, variable.VarId); 
            }
        }

        private QsFunc MakeFunc(
            QsFuncId funcId,
            bool bThisCall,            
            ImmutableArray<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> argTypeValues,
            QsTypeAndFuncBuilderContext context)
        {
            
            var func = new QsFunc(funcId, bThisCall, typeParams, retTypeValue, argTypeValues);
            context.Funcs.Add(func);

            return func;
        }

        private QsVariable MakeVar(
            QsVarId varId,
            QsTypeValue typeValue,
            QsTypeAndFuncBuilderContext context)
        {
            var variable = new QsVariable(varId, typeValue);
            context.Vars.Add(variable);

            return variable;
        }

        void BuildEnumDecl(QsEnumDecl enumDecl, QsTypeAndFuncBuilderContext context)
        {
            var typeId = context.TypeEvalInfo.TypeIdsByLocation[QsTypeIdLocation.Make(enumDecl)];
            
            var thisTypeValue = new QsNormalTypeValue(
                context.TypeBuilder?.ThisTypeValue,
                typeId,
                enumDecl.TypeParams.Select(typeParam => (QsTypeValue)new QsTypeVarTypeValue(typeId, typeParam)).ToImmutableArray());

            var prevTypeBuilder = context.TypeBuilder;
            context.TypeBuilder = new QsTypeBuilder(thisTypeValue);

            foreach (var elem in enumDecl.Elems)            
                BuildEnumDeclElement(elem, context);

            var enumType = new QsDefaultType(
                typeId,
                enumDecl.TypeParams,
                null, // TODO: Enum이던가 Object여야 한다,
                context.TypeBuilder.MemberTypeIds.ToImmutableDictionary(),
                context.TypeBuilder.StaticMemberFuncIds.ToImmutableDictionary(),
                context.TypeBuilder.StaticMemberVarIds.ToImmutableDictionary(),
                context.TypeBuilder.MemberFuncIds.ToImmutableDictionary(),
                context.TypeBuilder.MemberVarIds.ToImmutableDictionary());

            context.TypeBuilder = prevTypeBuilder;

            if (context.TypeBuilder == null)
                context.GlobalTypes.Add(enumType);

            context.Types.Add(enumType);
        }

        void BuildFuncDecl(QsFuncDecl funcDecl, QsTypeAndFuncBuilderContext context)
        {
            bool bThisCall = (context.TypeBuilder != null); // TODO: static 키워드가 추가되면 그때 다시 고쳐야 한다

            var func = MakeFunc(
                context.TypeEvalInfo.FuncIdsByLocation[QsFuncIdLocation.Make(funcDecl)],
                bThisCall,
                funcDecl.TypeParams,
                context.TypeEvalInfo.TypeValuesByTypeExp[funcDecl.RetType],
                funcDecl.Params.Select(typeAndName => context.TypeEvalInfo.TypeValuesByTypeExp[typeAndName.Type]).ToImmutableArray(),
                context);

            if (context.TypeBuilder == null)
                context.GlobalFuncs.Add(func);
        }
        
        void BuildScript(QsScript script, QsTypeAndFuncBuilderContext context)
        {
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsEnumDeclScriptElement enumElem:
                        BuildEnumDecl(enumElem.EnumDecl, context);
                        break;

                    case QsFuncDeclScriptElement funcElem:
                        BuildFuncDecl(funcElem.FuncDecl, context);
                        break;
                        
                }
            }
        }

        public QsTypeBuildInfo BuildScript(QsScript script, QsTypeEvalInfo typeEvalInfo)
        {   
            var context = new QsTypeAndFuncBuilderContext(typeEvalInfo);

            BuildScript(script, context);

            return new QsTypeBuildInfo(
                context.Types.ToImmutableDictionary(type => type.TypeId),
                context.Funcs.ToImmutableDictionary(func => func.FuncId),
                new Dictionary<QsTypeExp, QsTypeValue>(typeEvalInfo.TypeValuesByTypeExp),
                context.Vars.ToDictionary(var => var.VarId));
        }
    }
}