using QuickSC.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;

namespace QuickSC.StaticAnalyzer
{
    public partial class QsTypeAndFuncBuilder
    {
        public QsTypeAndFuncBuilder()
        {   
        }
        
        void BuildEnumDeclElement(QsMetaItemId outerTypeId, QsEnumDeclElement elem, Context context)
        {
            var thisTypeValue = context.TypeBuilder!.ThisTypeValue;

            // 타입 추가
            var elemType = new QsDefaultTypeInfo(
                outerTypeId,
                context.TypeIdsByLocation[QsMetadataIdLocation.Make(elem)], 
                ImmutableArray<string>.Empty,                
                thisTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
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

                context.TypeBuilder.MemberFuncIds.Add(QsName.Text(elem.Name), func.FuncId);
            }
            else
            {
                // NOTICE : E.First 타입이 아니라 E 타입이다 var x = E.First; 에서 x는 E여야 하기 떄문
                var variable = MakeVar(
                    new QsMetaItemId(new QsMetaItemIdElem(elem.Name, 0)),
                    bStatic: true,
                    thisTypeValue,
                    context);

                context.TypeBuilder.MemberVarIds.Add(QsName.Text(elem.Name), variable.VarId); 
            }
        }

        private QsFuncInfo MakeFunc(
            QsFuncDecl? funcDecl,
            QsMetaItemId funcId,
            bool bSeqCall,
            bool bThisCall,            
            ImmutableArray<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> argTypeValues,
            Context context)
        {   
            var func = new QsFuncInfo(funcId, bSeqCall, bThisCall, typeParams, retTypeValue, argTypeValues);
            context.FuncInfos.Add(func);

            if (funcDecl != null)
                context.FuncsByFuncDecl[funcDecl] = func;

            return func;
        }

        private QsVarInfo MakeVar(
            QsMetaItemId varId,
            bool bStatic,
            QsTypeValue typeValue,
            Context context)
        {
            var variable = new QsVarInfo(varId, bStatic, typeValue);
            context.VarInfos.Add(variable);

            return variable;
        }

        void BuildEnumDecl(QsEnumDecl enumDecl, Context context)
        {
            var typeId = context.TypeIdsByLocation[QsMetadataIdLocation.Make(enumDecl)];
            
            var thisTypeValue = new QsTypeValue_Normal(
                context.TypeBuilder?.ThisTypeValue,
                typeId,
                enumDecl.TypeParams.Select(typeParam => (QsTypeValue)new QsTypeValue_TypeVar(typeId, typeParam)).ToImmutableArray());

            var prevTypeBuilder = context.TypeBuilder;
            context.TypeBuilder = new TypeBuilder(thisTypeValue);

            foreach (var elem in enumDecl.Elems)
                BuildEnumDeclElement(typeId, elem, context);

            var enumType = new QsDefaultTypeInfo(
                null, // TODO: 일단 최상위
                typeId,
                enumDecl.TypeParams,
                null, // TODO: Enum이던가 Object여야 한다,
                context.TypeBuilder.MemberTypeIds.Values.ToImmutableArray(),
                context.TypeBuilder.MemberFuncIds.Values.ToImmutableArray(),
                context.TypeBuilder.MemberVarIds.Values.ToImmutableArray());

            context.TypeBuilder = prevTypeBuilder;

            context.TypeInfos.Add(enumType);
        }

        void BuildFuncDecl(QsFuncDecl funcDecl, Context context)
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

        void BuildGlobalStmt(QsStmt stmt, Context context)
        {
            // TODO: public int x; 형식만 모듈단위 외부 전역변수로 노출시킨다

            //var varDeclStmt = stmt as QsVarDeclStmt;
            //if (varDeclStmt == null) return;

            //var typeValue = context.TypeValuesByTypeExp[varDeclStmt.VarDecl.Type];
            
            //foreach(var elem in varDeclStmt.VarDecl.Elems)
            //{
            //    // TODO: 인자 bStatic에 true/false가 아니라, Global이라고 체크를 해야 한다
            //    MakeVar(new QsMetaItemId(new QsMetaItemIdElem(elem.VarName)), bStatic: true, typeValue, context);
            //}
        }
        
        void BuildScript(QsScript script, Context context)
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

                    case QsStmtScriptElement stmtElem:
                        BuildGlobalStmt(stmtElem.Stmt, context);
                        break;
                }
            }
        }

        public Result BuildScript(string moduleName, QsScript script, QsTypeSkelCollectResult skelResult,  QsTypeEvalResult typeEvalResult)
        {   
            var context = new Context(moduleName, skelResult, typeEvalResult);

            BuildScript(script, context);

            return new Result(
                context.TypeInfos.ToImmutableArray(),
                context.FuncInfos.ToImmutableArray(),
                context.VarInfos.ToImmutableArray(),
                context.FuncsByFuncDecl.ToImmutableWithComparer());
        }
    }
}