﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using QuickSC.Syntax;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzer
    {
        QsCapturer capturer;
        QsExpAnalyzer expAnalyzer;
        QsStmtAnalyzer stmtAnalyzer;        

        public QsAnalyzer(QsCapturer capturer)
        {
            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)
            this.capturer = capturer;
            this.expAnalyzer = new QsExpAnalyzer(this);
            this.stmtAnalyzer = new QsStmtAnalyzer(this);
        }

        
        internal bool AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        {
            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가
            // 4. var x = 1, y = "string"; // 각각 한다

            var elemsBuilder = ImmutableArray.CreateBuilder<QsVarDeclInfo.Element>(varDecl.Elems.Length);
            var declTypeValue = context.GetTypeValueByTypeExp(varDecl.Type);

            foreach (var elem in varDecl.Elems)
            {
                if (elem.InitExp == null)
                {
                    if (declTypeValue is QsTypeValue_Var)
                    {
                        context.ErrorCollector.Add(elem, $"{elem.VarName}의 타입을 추론할 수 없습니다");
                        return false;
                    }
                    else
                    {
                        AddElement(elem.VarName, declTypeValue, context);
                    }
                }
                else
                {
                    if (!AnalyzeExp(elem.InitExp, context, out var initExpTypeValue))
                        return false;

                    // var 처리
                    QsTypeValue typeValue;
                    if (declTypeValue is QsTypeValue_Var)
                    {
                        typeValue = initExpTypeValue;
                    }
                    else
                    {
                        typeValue = declTypeValue;

                        if (!IsAssignable(declTypeValue, initExpTypeValue, context))
                            context.ErrorCollector.Add(elem, $"타입 {initExpTypeValue}의 값은 타입 {varDecl.Type}의 변수 {elem.VarName}에 대입할 수 없습니다.");
                    }

                    AddElement(elem.VarName, typeValue, context);
                }
            }

            context.AddNodeInfo(varDecl, new QsVarDeclInfo(elemsBuilder.MoveToImmutable()));
            return true;

            void AddElement(string name, QsTypeValue typeValue, QsAnalyzerContext context)
            {
                // TODO: globalScope에서 public인 경우는, globalStorage로 
                if (context.IsGlobalScope())
                {
                    int varId = context.AddPrivateGlobalVarInfo(name, typeValue);
                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, QsStorage.MakePrivateGlobal(varId)));
                }
                else
                {
                    int localVarIndex = context.AddLocalVarInfo(name, typeValue);
                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, QsStorage.MakeLocal(localVarIndex)));
                }
            }
        }        

        public bool AnalyzeStringExpElement(QsStringExpElement elem, QsAnalyzerContext context)
        {
            bool bResult = true;

            if (elem is QsExpStringExpElement expElem)
            {
                // TODO: exp의 결과 string으로 변환 가능해야 하는 조건도 고려해야 한다
                if (AnalyzeExp(expElem.Exp, context, out var expTypeValue))
                {
                    context.AddNodeInfo(elem, new QsExpStringExpElementInfo(expTypeValue));
                }
                else
                {
                    bResult = false;
                }
            }

            return bResult;
        }

        public bool AnalyzeLambda(
            QsStmt body,
            ImmutableArray<QsLambdaExpParam> parameters,
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsCaptureInfo? outCaptureInfo,
            [NotNullWhen(returnValue: true)] out QsTypeValue_Func? outFuncTypeValue,
            out int outLocalVarCount)
        {
            outCaptureInfo = null;
            outFuncTypeValue = null;
            outLocalVarCount = 0;

            // capture에 필요한 정보를 가져옵니다
            if (!capturer.Capture(parameters.Select(param => param.Name), body, out var captureResult))
            {
                context.ErrorCollector.Add(body, "변수 캡쳐에 실패했습니다");
                return false;
            }

            // 람다 함수 컨텍스트를 만든다
            var lambdaFuncId = context.MakeLabmdaFuncId();

            // 캡쳐된 variable은 새 VarId를 가져야 한다
            var funcContext = new QsAnalyzerFuncContext(lambdaFuncId, null, false);

            // 필요한 변수들을 찾는다
            var elemsBuilder = ImmutableArray.CreateBuilder<QsCaptureInfo.Element>();
            foreach (var needCapture in captureResult.NeedCaptures)
            {
                if (context.GetIdentifierInfo(needCapture.VarName, ImmutableArray<QsTypeValue>.Empty, out var idInfo))
                {
                    if (idInfo is QsAnalyzerIdentifierInfo.Var varIdInfo)
                    {
                        switch (varIdInfo.Storage)
                        {
                            // 지역 변수라면 
                            case QsStorage.Local localStorage:
                                elemsBuilder.Add(new QsCaptureInfo.Element(needCapture.Kind, localStorage));
                                funcContext.AddLocalVarInfo(needCapture.VarName, varIdInfo.TypeValue);
                                break;

                            case QsStorage.ModuleGlobal moduleGlobalStorage:
                            case QsStorage.PrivateGlobal privateGlobalStorage:
                                break;

                            default:
                                throw new InvalidOperationException();
                        }

                        continue;
                    }
                }

                context.ErrorCollector.Add(body, "캡쳐실패");
                return false;                
            }            

            var paramTypeValuesBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(parameters.Length);
            foreach (var param in parameters)
            {
                if (param.Type == null)
                {
                    context.ErrorCollector.Add(param, "람다 인자 타입추론은 아직 지원하지 않습니다");
                    return false;
                }

                var paramTypeValue = context.GetTypeValueByTypeExp(param.Type);

                paramTypeValuesBuilder.Add(paramTypeValue);
                funcContext.AddLocalVarInfo(param.Name, paramTypeValue);
            }

            bool bResult = true;

            context.ExecInFuncScope(funcContext, () =>
            {
                bResult &= AnalyzeStmt(body, context);
            });

            outCaptureInfo = new QsCaptureInfo(false, elemsBuilder.ToImmutable());
            outFuncTypeValue = new QsTypeValue_Func(
                funcContext.GetRetTypeValue() ?? QsTypeValue_Void.Instance,
                paramTypeValuesBuilder.MoveToImmutable());
            outLocalVarCount = funcContext.GetLocalVarCount();

            return bResult;
        }

        internal bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            return expAnalyzer.AnalyzeExp(exp, context, out typeValue);
        }

        public bool AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            return stmtAnalyzer.AnalyzeStmt(stmt, context);
        }
        
        public bool AnalyzeFuncDecl(QsFuncDecl funcDecl, QsAnalyzerContext context)
        {
            var funcInfo = context.GetFuncInfoByFuncDecl(funcDecl);

            var bResult = true;
            
            var funcContext = new QsAnalyzerFuncContext(funcInfo.FuncId, funcInfo.RetTypeValue, funcInfo.bSeqCall);

            
            context.ExecInFuncScope(funcContext, () =>
            {   
                if (0 < funcDecl.TypeParams.Length || funcDecl.VariadicParamIndex != null)
                    throw new NotImplementedException();
                
                // 파라미터 순서대로 추가
                foreach (var param in funcDecl.Params)
                {
                    var paramTypeValue = context.GetTypeValueByTypeExp(param.Type);
                    context.AddLocalVarInfo(param.Name, paramTypeValue);
                }

                bResult &= AnalyzeStmt(funcDecl.Body, context);

                // TODO: Body가 실제로 리턴을 제대로 하는지 확인해야 할 필요가 있다

                context.AddFuncTemplate(funcInfo.FuncId, new QsScriptFuncTemplate.FuncDecl(
                    funcDecl.FuncKind == QsFuncKind.Sequence ? funcInfo.RetTypeValue : null,
                    funcInfo.bThisCall, context.GetLocalVarCount(), funcDecl.Body));
            });

            return bResult;
        }

        bool AnalyzeScript(QsScript script, QsAnalyzerContext context)
        {
            bool bResult = true;

            // 4. 최상위 script를 분석한다
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsStmtScriptElement stmtElem: 
                        bResult &= AnalyzeStmt(stmtElem.Stmt, context); 
                        break;
                }
            }

            // 5. 각 func body를 분석한다 (4에서 얻게되는 글로벌 변수 정보가 필요하다)
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    // TODO: classDecl
                    case QsFuncDeclScriptElement funcElem: 
                        bResult &= AnalyzeFuncDecl(funcElem.FuncDecl, context);
                        break;
                }
            }

            context.AddNodeInfo(script, new QsScriptInfo(context.GetLocalVarCount()));

            return bResult;
        }

        public bool AnalyzeScript(            
            QsScript script,
            QsMetadataService metadataService,
            QsTypeValueService typeValueService,
            QsTypeEvalResult evalResult,
            QsTypeAndFuncBuilder.Result buildResult,            
            IQsErrorCollector errorCollector,
            [NotNullWhen(returnValue: true)] out QsAnalyzeInfo? outInfo)
        {
            var context = new QsAnalyzerContext(
                metadataService,
                typeValueService,
                evalResult,
                buildResult,
                errorCollector);

            bool bResult = AnalyzeScript(script, context);

            if (!bResult || errorCollector.HasError)
            {
                outInfo = null;
                return false;
            }

            outInfo = new QsAnalyzeInfo(context.GetPrivateGlobalVarCount(), context.MakeInfosByNode(), context.MakeFuncTemplatesById());
            return true;
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, QsAnalyzerContext context)
        {
            // B <- D
            // 지금은 fromType의 base들을 찾아가면서 toTypeValue와 맞는 것이 있는지 본다
            // TODO: toTypeValue가 interface라면, fromTypeValue의 interface들을 본다

            QsTypeValue? curType = fromTypeValue;
            while (curType != null)
            {
                if (EqualityComparer<QsTypeValue>.Default.Equals(toTypeValue, curType))
                    return true;

                if (!context.TypeValueService.GetBaseTypeValue(curType, out var outType))
                    return false;

                curType = outType;
            }

            return false;
        }

        public QsTypeValue GetIntTypeValue()
        {
            return new QsTypeValue_Normal(null, new QsMetaItemId(new QsMetaItemIdElem("int")));
        }

        public QsTypeValue GetBoolTypeValue()
        {
            return new QsTypeValue_Normal(null, new QsMetaItemId(new QsMetaItemIdElem("bool")));
        }

        public QsTypeValue GetStringTypeValue()
        {
            return new QsTypeValue_Normal(null, new QsMetaItemId(new QsMetaItemIdElem("string"))); ;
        }
    }
}
