using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        QsAnalyzerTypeService typeService;

        public QsAnalyzer(QsCapturer capturer)
        {
            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)
            this.capturer = capturer;
            this.typeService = new QsAnalyzerTypeService();
            this.expAnalyzer = new QsExpAnalyzer(this, typeService);
            this.stmtAnalyzer = new QsStmtAnalyzer(this, typeService);
        }

        
        internal void AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        {
            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가
            // 4. var x = 1, y = "string"; // 각각 한다

            var elemsBuilder = ImmutableArray.CreateBuilder<QsVarDeclInfo.Element>(varDecl.Elems.Length);
            var declTypeValue = context.TypeValuesByTypeExp[varDecl.Type];

            foreach (var elem in varDecl.Elems)
            {
                if (elem.InitExp == null)
                {
                    if (declTypeValue is QsVarTypeValue)
                    {
                        context.ErrorCollector.Add(elem, $"{elem.VarName}의 타입을 추론할 수 없습니다");
                        return;
                    }
                    else
                    {
                        AddElement(elem.VarName, declTypeValue, context);
                    }
                }
                else
                {
                    if (!AnalyzeExp(elem.InitExp, context, out var initExpTypeValue))
                        return;

                    // var 처리
                    QsTypeValue typeValue;
                    if (declTypeValue is QsVarTypeValue)
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

            context.InfosByNode[varDecl] = new QsVarDeclInfo(elemsBuilder.MoveToImmutable());
            return;

            void AddElement(string name, QsTypeValue typeValue, QsAnalyzerContext context)
            {
                if (context.bGlobalScope)
                {
                    var varId = new QsVarId(context.ModuleName, ImmutableArray.Create(new QsNameElem(name, 0)));
                    var variable = new QsVariable(true, varId, typeValue);
                    typeService.AddVar(variable, context);

                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, new QsGlobalStorage(varId)));
                }
                else
                {
                    int localVarIndex = context.CurFunc.AddVarInfo(name, typeValue);
                    elemsBuilder.Add(new QsVarDeclInfo.Element(typeValue, new QsLocalStorage(localVarIndex)));
                }
            }
        }

        public void AnalyzeStringExpElement(QsStringExpElement elem, QsAnalyzerContext context)
        {
            if (elem is QsExpStringExpElement expElem)
            {
                // TODO: exp의 결과 string으로 변환 가능해야 하는 조건도 고려해야 한다
                AnalyzeExp(expElem.Exp, context, out var _);
            }
        }

        public bool AnalyzeLambda(
            QsStmt body,
            ImmutableArray<QsLambdaExpParam> parameters,
            QsAnalyzerContext context,
            [NotNullWhen(returnValue: true)] out QsCaptureInfo? outCaptureInfo,
            [NotNullWhen(returnValue: true)] out QsFuncTypeValue? outFuncTypeValue,
            out int outLocalVarCount)
        {
            outCaptureInfo = null;
            outFuncTypeValue = null;
            outLocalVarCount = 0;

            // capture에 필요한 정보를 가져옵니다
            if (!capturer.Capture(body, out var captureResult))
            {
                context.ErrorCollector.Add(body, "변수 캡쳐에 실패했습니다");
                return false;
            }

            // 람다 함수 컨텍스트를 만든다
            var lambdaFuncId = new QsFuncId(context.ModuleName, context.CurFunc.FuncId.Elems.Add(
                    new QsNameElem(QsName.AnonymousLambda(context.CurFunc.LambdaCount.ToString()), 0)));
            context.CurFunc.LambdaCount++;

            // 캡쳐된 variable은 새 VarId를 가져야 한다
            var func = new QsAnalyzerFuncContext(lambdaFuncId, null, false);

            var (prevFunc, bPrevGlobalScope) = (context.CurFunc, context.bGlobalScope);
            context.bGlobalScope = false;
            context.CurFunc = func;

            // 필요한 변수들을 찾는다
            var elemsBuilder = ImmutableArray.CreateBuilder<QsCaptureInfo.Element>(captureResult.NeedCaptures.Length);
            foreach (var needCapture in captureResult.NeedCaptures)
            {
                if (context.CurFunc.GetVarInfo(needCapture.VarName, out var localVarInfo))
                {
                    elemsBuilder.Add(new QsCaptureInfo.Element(needCapture.Kind, new QsLocalStorage(localVarInfo.Index)));
                    context.CurFunc.AddVarInfo(needCapture.VarName, localVarInfo.TypeValue);
                }
                else if (typeService.GetGlobalVar(needCapture.VarName, context, out var globalVar))
                {
                    continue;
                    // TODO: 람다에서 글로벌 변수는 캡쳐하지 않는다 QsLambdaExpInfo.Elem.MakeGlobal 제거
                    // elemsBuilder.Add(QsLambdaExpInfo.Elem.MakeGlobal(needCapture.Kind, globalVar.VarId));
                    // context.CurFunc.AddVarInfo(needCapture.VarName, globalVar.TypeValue);
                }
                else
                {
                    context.ErrorCollector.Add(body, "캡쳐실패");
                    return false;
                }
            }

            var paramTypeValuesBuilder = ImmutableArray.CreateBuilder<QsTypeValue>(parameters.Length);
            foreach (var param in parameters)
            {
                if (param.Type == null)
                {
                    context.ErrorCollector.Add(param, "람다 인자 타입추론은 아직 지원하지 않습니다");
                    return false;
                }

                var paramTypeValue = context.TypeValuesByTypeExp[param.Type];

                paramTypeValuesBuilder.Add(paramTypeValue);
                context.CurFunc.AddVarInfo(param.Name, paramTypeValue);
            }

            AnalyzeStmt(body, context);

            context.bGlobalScope = bPrevGlobalScope;
            context.CurFunc = prevFunc;

            outCaptureInfo = new QsCaptureInfo(false, elemsBuilder.MoveToImmutable());
            outFuncTypeValue = new QsFuncTypeValue(
                func.RetTypeValue ?? QsVoidTypeValue.Instance,
                paramTypeValuesBuilder.MoveToImmutable());
            outLocalVarCount = func.LocalVarCount;
            return true;
        }

        internal bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        {
            return expAnalyzer.AnalyzeExp(exp, context, out typeValue);
        }

        public void AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            stmtAnalyzer.AnalyzeStmt(stmt, context);
        }
        
        public void AnalyzeFuncDecl(QsFuncDecl funcDecl, QsAnalyzerContext context)
        {
            var func = context.FuncsByFuncDecl[funcDecl];

            var funcContext = new QsAnalyzerFuncContext(func.FuncId, func.RetTypeValue, funcDecl.FuncKind == QsFuncKind.Sequence);
            var prevFunc = context.CurFunc;
            context.CurFunc = funcContext;

            if (0 < funcDecl.TypeParams.Length || funcDecl.VariadicParamIndex != null)
                throw new NotImplementedException();

            try
            {   
                // 파라미터 순서대로 추가
                foreach(var param in funcDecl.Params)
                {
                    var paramTypeValue = context.TypeValuesByTypeExp[param.Type];
                    funcContext.AddVarInfo(param.Name, paramTypeValue);
                }

                AnalyzeStmt(funcDecl.Body, context);

                // TODO: Body가 실제로 리턴을 제대로 하는지 확인해야 할 필요가 있다

                context.FuncTemplatesById[func.FuncId] = new QsScriptFuncTemplate.FuncDecl(
                    funcDecl.FuncKind == QsFuncKind.Sequence ? func.RetTypeValue : null,
                    func.bThisCall, funcContext.LocalVarCount, funcDecl.Body);
            }
            finally
            {
                context.CurFunc = prevFunc;
            }
        }

        QsAnalyzeInfo AnalyzeScript(QsScript script, QsAnalyzerContext context)
        {   
            // 4. 최상위 script를 분석한다
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsStmtScriptElement stmtElem: 
                        AnalyzeStmt(stmtElem.Stmt, context); 
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
                        AnalyzeFuncDecl(funcElem.FuncDecl, context);
                        break;
                }
            }

            context.InfosByNode[script] = new QsScriptInfo(context.CurFunc.LocalVarCount);

            return new QsAnalyzeInfo(
                context.InfosByNode.ToImmutableWithComparer(),
                context.FuncTemplatesById.ToImmutableDictionary());
        }

        public bool AnalyzeScript(
            string moduleName,
            QsScript script,
            ImmutableArray<IQsMetadata> metadatas,
            QsTypeEvalResult evalResult,
            QsTypeAndFuncBuildResult buildResult,            
            IQsErrorCollector errorCollector,
            [NotNullWhen(returnValue: true)] out QsAnalyzeInfo? outInfo)
        {
            var context = new QsAnalyzerContext(
                moduleName,
                metadatas,
                evalResult,
                buildResult,
                errorCollector);

            AnalyzeScript(script, context);

            if (errorCollector.HasError)
            {
                outInfo = null;
                return false;
            }

            outInfo = new QsAnalyzeInfo(context.InfosByNode.ToImmutableWithComparer(), context.FuncTemplatesById.ToImmutableDictionary());
            return true;
        }

        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, QsAnalyzerContext context)
        {
            return typeService.IsAssignable(toTypeValue, fromTypeValue, context);
        }
    }
}
