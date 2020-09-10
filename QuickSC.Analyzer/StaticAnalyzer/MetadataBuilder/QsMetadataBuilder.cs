using Gum.Syntax;
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

        private QsFuncInfo MakeFunc(
            FuncDecl? funcDecl,
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

        void BuildEnumDecl(EnumDecl enumDecl, Context context)
        {
            QsEnumElemInfo MakeElemInfo(QsMetaItemId enumTypeId, EnumDeclElement elem, Context context)
            {
                var fieldInfos = elem.Params.Select(parameter =>
                {
                    var typeValue = context.GetTypeValue(parameter.Type);
                    return new QsEnumElemFieldInfo(typeValue, parameter.Name);
                });

                return new QsEnumElemInfo(elem.Name, fieldInfos);
            }
            
            var typeId = context.GetTypeId(enumDecl);
            
            var elemInfos = enumDecl.Elems.Select(elem => MakeElemInfo(typeId, elem, context));

            var enumType = new QsEnumInfo(
                context.GetThisTypeValue()?.TypeId,
                typeId,
                enumDecl.TypeParams, elemInfos);

            context.AddEnumInfo(enumDecl, enumType);
        }

        void BuildFuncDecl(FuncDecl funcDecl, Context context)
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
                funcDecl.FuncKind == FuncKind.Sequence,
                bThisCall,
                funcDecl.TypeParams,
                context.GetTypeValue(funcDecl.RetType),
                funcDecl.Params.Select(typeAndName => context.GetTypeValue(typeAndName.Type)).ToImmutableArray(),
                context);
        }

        void BuildGlobalStmt(Stmt stmt, Context context)
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
        
        void BuildScript(Script script, Context context)
        {
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case EnumDeclScriptElement enumElem:
                        BuildEnumDecl(enumElem.EnumDecl, context);
                        break;

                    case FuncDeclScriptElement funcElem:
                        BuildFuncDecl(funcElem.FuncDecl, context);
                        break;

                    case StmtScriptElement stmtElem:
                        BuildGlobalStmt(stmtElem.Stmt, context);
                        break;
                }
            }
        }

        public Result? BuildScript(string moduleName, IEnumerable<IQsMetadata> metadatas, Script script, IQsErrorCollector errorCollector)
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
                context.GetFuncsByFuncDecl(),
                context.GetEnumInfosByDecl());
        }
    }
}