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
        QsExpAnalyzer expAnalyzer;
        QsStmtAnalyzer stmtAnalyzer;
        QsAnalyzerTypeService typeService;

        public QsAnalyzer(QsCapturer capturer)
        {
            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)
            this.typeService = new QsAnalyzerTypeService();
            this.expAnalyzer = new QsExpAnalyzer(this, capturer, typeService);
            this.stmtAnalyzer = new QsStmtAnalyzer(this, typeService);
        }

        internal bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsTypeValue? typeValue)
        { 
            return expAnalyzer.AnalyzeExp(exp, context, out typeValue);
        }

        // TODO: func도 지원해야 한다
        bool MakeVarStorage(string id, QsAnalyzerContext context, [NotNullWhen(returnValue: true)] out QsStorage? outStorage)
        {
            if (context.CurFunc.GetVarInfo(id, out var localVarInfo))
            {
                outStorage = new QsLocalVarStorage(localVarInfo.Index);
                return true;
            }

            // TODO: this도 검색, This도 검색
            if (typeService.GetGlobalVar(id, context, out var globalVar))
            {
                // GlobalVar
                outStorage = new QsGlobalVarStorage(globalVar.VarId);
                return true;
            }

            outStorage = null;
            return false;
        }
        
        // TODO: QsVariable, LocalIndex가 Storage가 다른 것은 무엇인가
        //public QsStorage AddVariable(string name, QsTypeValue typeValue, QsAnalyzerContext context)
        //{
        //    if (context.bGlobalScope)
        //    {
        //        var varId = new QsVarId(null, ImmutableArray.Create(new QsNameElem(name, 0)));
        //        var variable = new QsVariable(varId, typeValue);
        //        typeValueService.AddVar(variable, context.TypeValueServiceContext);
        //        return new QsGlobalVarStorage(varId);
        //    }
        //    else
        //    {
        //        int localVarIndex = context.CurFunc.AddVarInfo(name, typeValue);
        //        return new QsLocalVarStorage(localVarIndex);
        //    }
        //}

        abstract class QsEvalVarDeclBuilder
        {
            public abstract void Add(string name, QsTypeValue typeValue, QsExp? initExp, QsAnalyzerContext context);
            public abstract QsEvalVarDecl Build();
        }

        class QsGlobalVarDeclBuilder : QsEvalVarDeclBuilder
        {
            ImmutableArray<QsGlobalVarDecl.Elem>.Builder elemsBuilder;
            QsAnalyzerTypeService typeService;

            public QsGlobalVarDeclBuilder(QsAnalyzerTypeService typeService, int capacity)
            {
                this.typeService = typeService;
                elemsBuilder = ImmutableArray.CreateBuilder<QsGlobalVarDecl.Elem>(capacity);
            }

            public override void Add(string name, QsTypeValue typeValue, QsExp? initExp, QsAnalyzerContext context)
            {
                var varId = new QsVarId(null, ImmutableArray.Create(new QsNameElem(name, 0)));
                var variable = new QsVariable(varId, typeValue);
                typeService.AddVar(variable, context);

                elemsBuilder.Add(new QsGlobalVarDecl.Elem(typeValue, varId, initExp));
            }

            public override QsEvalVarDecl Build()
            {
                return new QsGlobalVarDecl(elemsBuilder.MoveToImmutable());
            }
        }

        class QsLocalVarDeclBuilder : QsEvalVarDeclBuilder
        {
            ImmutableArray<QsLocalVarDecl.Elem>.Builder elemsBuilder;
            public QsLocalVarDeclBuilder(int capacity)
            {
                elemsBuilder = ImmutableArray.CreateBuilder<QsLocalVarDecl.Elem>(capacity);
            }

            public override void Add(string name, QsTypeValue typeValue, QsExp? initExp, QsAnalyzerContext context)
            {
                int localVarIndex = context.CurFunc.AddVarInfo(name, typeValue);
                elemsBuilder.Add(new QsLocalVarDecl.Elem(typeValue, localVarIndex, initExp));
            }

            public override QsEvalVarDecl Build()
            {
                return new QsLocalVarDecl(elemsBuilder.MoveToImmutable());
            }
        }
        
        internal void AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        {
            QsEvalVarDeclBuilder varDeclBuilder;
            if (context.bGlobalScope)
                varDeclBuilder = new QsGlobalVarDeclBuilder(typeService, varDecl.Elements.Length);
            else
                varDeclBuilder = new QsLocalVarDeclBuilder(varDecl.Elements.Length);

            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가
            // 4. var x = 1, y = "string"; // 각각 한다

            // TODO: 추후에는 매번 만들지 않고, QsAnalyzerContext안에서 직접 관리한다
            var declTypeValue = context.TypeBuildInfo.TypeValuesByTypeExp[varDecl.Type];

            foreach (var elem in varDecl.Elements)
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
                        varDeclBuilder.Add(elem.VarName, declTypeValue, elem.InitExp, context);
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

                    varDeclBuilder.Add(elem.VarName, declTypeValue, elem.InitExp, context);                    
                }
            }

            context.EvalVarDeclsByVarDecl[varDecl] = varDeclBuilder.Build();
        }
        
        public void AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            stmtAnalyzer.AnalyzeStmt(stmt, context);
        }
        
        public void AnalyzeFuncDecl(QsFuncDecl funcDecl, QsAnalyzerContext context)
        {
            throw new NotImplementedException();
        }

        public QsAnalyzeInfo AnalyzeScript(QsScript script, QsAnalyzerContext context)
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

            return new QsAnalyzeInfo(
                context.TypeValuesByExp.ToImmutableDictionary(),
                context.TypeBuildInfo.TypeValuesByTypeExp.ToImmutableDictionary(),
                context.FuncValuesByExp.ToImmutableDictionary(),
                context.ForeachInfosByForEachStmt.ToImmutableDictionary(),
                context.EvalVarDeclsByVarDecl.ToImmutableDictionary(),
                context.EvalInfosByNode.ToImmutableDictionary());
        }

        public bool AnalyzeScript(
            QsScript script,
            ImmutableArray<IQsMetadata> metadatas,
            QsTypeBuildInfo typeBuildInfo,
            IQsErrorCollector errorCollector)
        {
            var context = new QsAnalyzerContext(
                metadatas,
                typeBuildInfo,
                errorCollector);


            AnalyzeScript(script, context);

            if (errorCollector.HasError)
                return false;



            throw new NotImplementedException();
        }


        public bool IsAssignable(QsTypeValue toTypeValue, QsTypeValue fromTypeValue, QsAnalyzerContext context)
        {
            return typeService.IsAssignable(toTypeValue, fromTypeValue, context);
        }
    }
}
