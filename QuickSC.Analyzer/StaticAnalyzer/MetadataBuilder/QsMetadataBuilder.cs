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
    public partial class QsMetadataBuilder
    {
        QsTypeExpEvaluator typeExpEvaluator;

        public QsMetadataBuilder(QsTypeExpEvaluator typeExpEvaluator)
        {
            this.typeExpEvaluator = typeExpEvaluator;
        }
        
        void BuildEnumDeclElement(QsMetaItemId outerTypeId, QsEnumDeclElement elem, Context context)
        {
            // TODO: 새로 기획한 방식대로 다시 작성할 것
            //var thisTypeValue = context.TypeBuilder!.ThisTypeValue;

            //// 타입 추가
            //var elemType = new QsDefaultTypeInfo(
            //    outerTypeId,
            //    context.GetTypeId(elem), 
            //    ImmutableArray<string>.Empty,                
            //    thisTypeValue, // enum E<T>{ First } => E<T>.First : E<T>
            //    ImmutableArray<QsMetaItemId>.Empty,
            //    ImmutableArray<QsMetaItemId>.Empty,
            //    ImmutableArray<QsMetaItemId>.Empty);

            //context.TypeBuilder.MemberTypeIds.Add(QsName.Text(elem.Name), elemType.TypeId);

            //if (0 < elem.Params.Length)
            //{
            //    var argTypes = ImmutableArray.CreateBuilder<QsTypeValue>(elem.Params.Length);
            //    foreach (var param in elem.Params)
            //    {
            //        var typeValue = context.GetTypeValue(param.Type);
            //        argTypes.Add(typeValue);
            //    }

            //    // Func를 만들까 FuncSkeleton을 만들까
            //    var func = MakeFunc(
            //        null,
            //        outerTypeId,
            //        context.GetFuncId(elem),
            //        false,
            //        false, 
            //        ImmutableArray<string>.Empty, 
            //        thisTypeValue, 
            //        argTypes.MoveToImmutable(), 
            //        context);

            //    context.TypeBuilder.MemberFuncIds.Add(QsName.Text(elem.Name), func.FuncId);
            //}
            //else
            //{
            //    // NOTICE : E.First 타입이 아니라 E 타입이다 var x = E.First; 에서 x는 E여야 하기 떄문
            //    var variable = MakeVar(
            //        QsMetaItemId.Make(new QsMetaItemIdElem(elem.Name, 0)),
            //        bStatic: true,
            //        thisTypeValue,
            //        context);

            //    context.TypeBuilder.MemberVarIds.Add(QsName.Text(elem.Name), variable.VarId); 
            //}
        }

        private QsFuncInfo MakeFunc(
            QsFuncDecl? funcDecl,
            QsMetaItemId? outerId,
            QsMetaItemId funcId,
            bool bSeqCall,
            bool bThisCall,            
            ImmutableArray<string> typeParams,
            QsTypeValue retTypeValue,
            ImmutableArray<QsTypeValue> argTypeValues,
            Context context)
        {   
            var funcInfo = new QsFuncInfo(outerId, funcId, bSeqCall, bThisCall, typeParams, retTypeValue, argTypeValues);
            context.AddFuncInfo(funcDecl, funcInfo);
            return funcInfo;
        }

        private QsVarInfo MakeVar(
            QsMetaItemId? outerId,
            QsMetaItemId varId,
            bool bStatic,
            QsTypeValue typeValue,
            Context context)
        {
            var varInfo = new QsVarInfo(outerId, varId, bStatic, typeValue);
            context.AddVarInfo(varInfo);

            return varInfo;
        }

        void BuildEnumDecl(QsEnumDecl enumDecl, Context context)
        {
            // TODO: Enum다시 만들기
            //var typeId = context.GetTypeId(enumDecl);
            
            //var thisTypeValue = QsTypeValue.MakeNormal(
            //    context.TypeBuilder?.ThisTypeValue,
            //    typeId,
            //    enumDecl.TypeParams.Select(typeParam => (QsTypeValue)QsTypeValue.MakeTypeVar(typeId, typeParam)).ToImmutableArray());

            //var prevTypeBuilder = context.TypeBuilder;
            //context.TypeBuilder = new TypeBuilder(thisTypeValue);

            //foreach (var elem in enumDecl.Elems)
            //    BuildEnumDeclElement(typeId, elem, context);

            //var enumType = new QsDefaultTypeInfo(
            //    null, // TODO: 일단 최상위
            //    typeId,
            //    enumDecl.TypeParams,
            //    null, // TODO: Enum이던가 Object여야 한다,
            //    context.TypeBuilder.MemberTypeIds.Values.ToImmutableArray(),
            //    context.TypeBuilder.MemberFuncIds.Values.ToImmutableArray(),
            //    context.TypeBuilder.MemberVarIds.Values.ToImmutableArray());

            //context.TypeBuilder = prevTypeBuilder;

            //context.TypeInfos.Add(enumType);
        }

        void BuildFuncDecl(QsFuncDecl funcDecl, Context context)
        {
            var thisTypeValue = context.GetThisTypeValue();            

            QsMetaItemId? outerId = null;
            bool bThisCall = false;

            if (thisTypeValue != null)
            {
                outerId = thisTypeValue.TypeId;
                bThisCall = true; // TODO: static 키워드가 추가되면 그때 다시 고쳐야 한다
            }

            var func = MakeFunc(
                funcDecl,
                outerId,                
                context.GetFuncId(funcDecl),
                funcDecl.FuncKind == QsFuncKind.Sequence,
                bThisCall,
                funcDecl.TypeParams,
                context.GetTypeValue(funcDecl.RetType),
                funcDecl.Params.Select(typeAndName => context.GetTypeValue(typeAndName.Type)).ToImmutableArray(),
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
            //    MakeVar(QsMetaItemId.Make(new QsMetaItemIdElem(elem.VarName)), bStatic: true, typeValue, context);
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

        public Result? BuildScript(string moduleName, IEnumerable<IQsMetadata> metadatas, QsScript script, IQsErrorCollector errorCollector)
        {
            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            var typeEvalResult = typeExpEvaluator.EvaluateScript(script, metadatas, errorCollector);
            if (typeEvalResult == null)
                return null;

            var context = new Context(typeEvalResult.Value.SyntaxNodeMetaItemService, typeEvalResult.Value.TypeExpTypeValueService);

            BuildScript(script, context);

            var scriptMetadata = new QsScriptMetadata(
                moduleName, 
                context.GetTypeInfos(), 
                context.GetFuncInfos(), 
                context.GetVarInfos());

            return new Result(
                scriptMetadata,
                typeEvalResult.Value.TypeExpTypeValueService,
                context.GetFuncsByFuncDecl());
        }
    }
}