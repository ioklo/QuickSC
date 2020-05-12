using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace QuickSC.StaticAnalyzer
{
    class QsTypeAndFuncBuilderContext
    {
        public ImmutableDictionary<QsTypeDecl, QsTypeId> TypeIdsByTypeDecl { get; }
        public ImmutableDictionary<QsTypeExp, QsTypeValue> TypeValuesByTypeExp { get; }
        public QsTypeValue? ScopeType { get; set; }
        public Dictionary<QsTypeId, QsType> Types { get;}
        public Dictionary<QsFuncId, QsFunc> Funcs { get; }
        public Dictionary<string, QsFunc> GlobalFuncs { get; }

        public QsTypeAndFuncBuilderContext(
            ImmutableDictionary<QsTypeDecl, QsTypeId> typeIdsByTypeDecl,
            ImmutableDictionary<QsTypeExp, QsTypeValue> typeValuesByTypeExp)
        {
            TypeIdsByTypeDecl = typeIdsByTypeDecl;
            TypeValuesByTypeExp = typeValuesByTypeExp;

            ScopeType = null;
            Types = new Dictionary<QsTypeId, QsType>();
            Funcs = new Dictionary<QsFuncId, QsFunc>();
            GlobalFuncs = new Dictionary<string, QsFunc>();
        }
    }

    // TODO: 이름을 TypeAndFuncBuilder로..
    internal class QsTypeAndFuncBuilder
    {
        QsFuncIdFactory funcIdFactory;

        public QsTypeAndFuncBuilder(QsFuncIdFactory funcIdFactory)
        {
            this.funcIdFactory = funcIdFactory;
        }

        static QsType BuildEnumElemType(QsEnumDeclElement elem, QsTypeValue baseTypeValue, QsTypeAndFuncBuilderContext context)
        {
            var type = new QsDefaultType(
                context.TypeIdsByTypeDecl[elem], 
                elem.Name, 
                ImmutableArray<string>.Empty,                
                baseTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
                ImmutableDictionary<string, QsTypeId>.Empty,
                ImmutableDictionary<QsMemberFuncId, QsFuncId>.Empty,
                ImmutableDictionary<string, QsTypeValue>.Empty);

            context.Types.Add(type.TypeId, type);
            return type;
        }

        private QsFunc MakeFunc(
            bool bThisCall,            
            string name,
            ImmutableArray<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> argTypeValues,
            QsTypeAndFuncBuilderContext context)
        {
            var funcId = funcIdFactory.MakeFuncId();
            var func = new QsFunc(funcId, bThisCall, name, typeParams, retTypeValue, argTypeValues);
            context.Funcs.Add(funcId, func);

            return func;
        }

        void BuildEnumDecl(QsEnumDecl enumDecl, QsTypeAndFuncBuilderContext context)
        {
            var typeId = context.TypeIdsByTypeDecl[enumDecl];

            var typeIdsBuilder = ImmutableDictionary.CreateBuilder<string, QsTypeId>();
            var funcIdsBuilder = ImmutableDictionary.CreateBuilder<QsMemberFuncId, QsFuncId>();
            var varTypeValuesBuilder = ImmutableDictionary.CreateBuilder<string, QsTypeValue>();

            var thisTypeValue = new QsNormalTypeValue(
                context.ScopeType,
                typeId,
                enumDecl.TypeParams.Select(typeParam => (QsTypeValue)new QsTypeVarTypeValue(this, typeParam)).ToImmutableArray());

            foreach (var elem in enumDecl.Elems)
            {
                var memberType = BuildEnumElemType(elem, thisTypeValue, context);
                typeIdsBuilder.Add(elem.Name, memberType.TypeId);

                if (0 < elem.Params.Length)
                {
                    var argTypes = ImmutableArray.CreateBuilder<QsTypeValue>(elem.Params.Length);
                    foreach (var param in elem.Params)
                    {
                        var typeValue = context.TypeValuesByTypeExp[param.Type];
                        argTypes.Add(typeValue);
                    }

                    // Func를 만들까 FuncSkeleton을 만들까
                    var func = MakeFunc(false, elem.Name, ImmutableArray<string>.Empty, thisTypeValue, argTypes.MoveToImmutable(), context);
                    funcIdsBuilder.Add(new QsMemberFuncId(func.Name), func.FuncId);
                }
                else
                {
                    // E.First
                    var varTypeValue = new QsNormalTypeValue(thisTypeValue, memberType.TypeId);
                    varTypeValuesBuilder.Add(elem.Name, varTypeValue);
                }
            }

            var enumType = new QsDefaultType(
                typeId,
                enumDecl.Name,
                enumDecl.TypeParams,
                null, // TODO: Enum이던가 Object여야 한다
                typeIdsBuilder.ToImmutable(),
                funcIdsBuilder.ToImmutable(),
                varTypeValuesBuilder.ToImmutable());

            context.Types.Add(enumType.TypeId, enumType);
        }

        void BuildFuncDecl(QsFuncDecl funcDecl, QsTypeAndFuncBuilderContext context)
        {
            bool bThisCall = (context.ScopeType != null); // TODO: static 키워드가 추가되면 그때 다시 고쳐야 한다

            var func = MakeFunc(
                bThisCall,
                funcDecl.Name,
                funcDecl.TypeParams,
                context.TypeValuesByTypeExp[funcDecl.RetType],
                funcDecl.Params.Select(typeAndName => context.TypeValuesByTypeExp[typeAndName.Type]).ToImmutableArray(),
                context);

            if (context.ScopeType == null)
                context.GlobalFuncs.Add(func.Name, func);
        }
        
        public void BuildScript(QsScript script, QsTypeAndFuncBuilderContext context)
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
    }
}