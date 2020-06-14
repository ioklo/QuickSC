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
        public string ModuleName { get; }

        public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> TypeIdsByLocation { get; }
        public ImmutableDictionary<QsMetadataIdLocation, QsMetaItemId> FuncIdsByLocation { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }

        public QsTypeBuilder? TypeBuilder { get; set; }
        public List<QsType> Types { get;} // All Types
        public List<QsFunc> Funcs { get; } // All Funcs
        public List<QsVariable> Vars { get; } // Type의 Variable
        public Dictionary<QsFuncDecl, QsFunc> FuncsByFuncDecl { get; }        

        public QsTypeAndFuncBuilderContext(
            string moduleName,
            QsTypeSkelCollectResult skelResult,
            QsTypeEvalResult evalResult)
        {
            ModuleName = moduleName;
            TypeIdsByLocation = skelResult.TypeIdsByLocation;
            FuncIdsByLocation = skelResult.FuncIdsByLocation;
            TypeValuesByTypeExp = evalResult.TypeValuesByTypeExp;

            TypeBuilder = null;
            Types = new List<QsType>();
            Funcs = new List<QsFunc>();
            Vars = new List<QsVariable>();
            FuncsByFuncDecl = new Dictionary<QsFuncDecl, QsFunc>(QsRefEqComparer<QsFuncDecl>.Instance);
        }
    }

    public class QsTypeAndFuncBuildResult
    {
        public ImmutableArray<QsType> Types { get; }
        public ImmutableArray<QsFunc> Funcs { get; }
        public ImmutableArray<QsVariable> Vars { get; }
        public ImmutableDictionary<QsFuncDecl, QsFunc> FuncsByFuncDecl { get; }

        public QsTypeAndFuncBuildResult(
            ImmutableArray<QsType> types,
            ImmutableArray<QsFunc> funcs,
            ImmutableArray<QsVariable> vars,
            ImmutableDictionary<QsFuncDecl, QsFunc> funcsByFuncDecl)
        {
            Types = types;
            Funcs = funcs;
            Vars = vars;

            FuncsByFuncDecl = funcsByFuncDecl;
        }
    }

    class QsTypeBuilder
    {
        public QsTypeValue ThisTypeValue { get; }
        public Dictionary<QsName, QsMetaItemId> MemberTypeIds { get; }
        public Dictionary<QsName, QsMetaItemId> StaticMemberFuncIds { get; }
        public Dictionary<QsName, QsMetaItemId> StaticMemberVarIds { get; }
        public Dictionary<QsName, QsMetaItemId> MemberFuncIds { get; }
        public Dictionary<QsName, QsMetaItemId> MemberVarIds { get; }

        public QsTypeBuilder(QsTypeValue thisTypeValue)
        {
            ThisTypeValue = thisTypeValue;

            MemberTypeIds = new Dictionary<QsName, QsMetaItemId>();
            StaticMemberFuncIds = new Dictionary<QsName, QsMetaItemId>();
            StaticMemberVarIds = new Dictionary<QsName, QsMetaItemId>();
            MemberFuncIds = new Dictionary<QsName, QsMetaItemId>();
            MemberVarIds = new Dictionary<QsName, QsMetaItemId>();
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
                context.TypeIdsByLocation[QsMetadataIdLocation.Make(elem)], 
                ImmutableArray<string>.Empty,                
                thisTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty,
                ImmutableArray<QsMetaItemId>.Empty);

            context.TypeBuilder.MemberTypeIds.Add(QsName.Text(elem.Name), elemType.TypeId);

            if (0 < elem.Params.Length)
            {
                var argTypes = ImmutableArray.CreateBuilder<QsTypeValue>(elem.Params.Length);
                foreach (var param in elem.Params)
                {
                    var typeValue = context.TypeValuesByTypeExp[param.Type];
                    argTypes.Add(typeValue);
                }

                // Func를 만들까 FuncSkeleton을 만들까
                var func = MakeFunc(
                    null,
                    context.FuncIdsByLocation[QsMetadataIdLocation.Make(elem)],
                    false,
                    false, 
                    ImmutableArray<string>.Empty, 
                    thisTypeValue, 
                    argTypes.MoveToImmutable(), 
                    context);

                context.TypeBuilder.StaticMemberFuncIds.Add(QsName.Text(elem.Name), func.FuncId);
            }
            else
            {
                // NOTICE : E.First 타입이 아니라 E 타입이다 var x = E.First; 에서 x는 E여야 하기 떄문
                var variable = MakeVar(
                    new QsMetaItemId(new QsMetaItemIdElem(elem.Name, 0)),
                    bStatic: true,
                    thisTypeValue,
                    context);

                context.TypeBuilder.StaticMemberVarIds.Add(QsName.Text(elem.Name), variable.VarId); 
            }
        }

        private QsFunc MakeFunc(
            QsFuncDecl? funcDecl,
            QsMetaItemId funcId,
            bool bSeqCall,
            bool bThisCall,            
            ImmutableArray<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> argTypeValues,
            QsTypeAndFuncBuilderContext context)
        {   
            var func = new QsFunc(funcId, bSeqCall, bThisCall, typeParams, retTypeValue, argTypeValues);
            context.Funcs.Add(func);

            if (funcDecl != null)
                context.FuncsByFuncDecl[funcDecl] = func;

            return func;
        }

        private QsVariable MakeVar(
            QsMetaItemId varId,
            bool bStatic,
            QsTypeValue typeValue,
            QsTypeAndFuncBuilderContext context)
        {
            var variable = new QsVariable(bStatic, varId, typeValue);
            context.Vars.Add(variable);

            return variable;
        }

        void BuildEnumDecl(QsEnumDecl enumDecl, QsTypeAndFuncBuilderContext context)
        {
            var typeId = context.TypeIdsByLocation[QsMetadataIdLocation.Make(enumDecl)];
            
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
                context.TypeBuilder.MemberTypeIds.Values.ToImmutableArray(),
                context.TypeBuilder.StaticMemberFuncIds.Values.ToImmutableArray(),
                context.TypeBuilder.StaticMemberVarIds.Values.ToImmutableArray(),
                context.TypeBuilder.MemberFuncIds.Values.ToImmutableArray(),
                context.TypeBuilder.MemberVarIds.Values.ToImmutableArray());

            context.TypeBuilder = prevTypeBuilder;

            context.Types.Add(enumType);
        }

        void BuildFuncDecl(QsFuncDecl funcDecl, QsTypeAndFuncBuilderContext context)
        {
            bool bThisCall = (context.TypeBuilder != null); // TODO: static 키워드가 추가되면 그때 다시 고쳐야 한다

            var func = MakeFunc(
                funcDecl,
                context.FuncIdsByLocation[QsMetadataIdLocation.Make(funcDecl)],
                funcDecl.FuncKind == QsFuncKind.Sequence,
                bThisCall,
                funcDecl.TypeParams,
                context.TypeValuesByTypeExp[funcDecl.RetType],
                funcDecl.Params.Select(typeAndName => context.TypeValuesByTypeExp[typeAndName.Type]).ToImmutableArray(),
                context);
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

        public QsTypeAndFuncBuildResult BuildScript(string moduleName, QsScript script, QsTypeSkelCollectResult skelResult,  QsTypeEvalResult typeEvalResult)
        {   
            var context = new QsTypeAndFuncBuilderContext(moduleName, skelResult, typeEvalResult);

            BuildScript(script, context);

            return new QsTypeAndFuncBuildResult(
                context.Types.ToImmutableArray(),
                context.Funcs.ToImmutableArray(),
                context.Vars.ToImmutableArray(),
                context.FuncsByFuncDecl.ToImmutableWithComparer());
        }
    }
}