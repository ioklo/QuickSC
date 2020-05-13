using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using QuickSC.Syntax;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzer
    {
        // QsExpAnalyzer expAnalyzer;
        QsStmtAnalyzer stmtAnalyzer;
        QsCapturer capturer;
        QsTypeValueService typeValueService;

        public QsAnalyzer(QsCapturer capturer, QsTypeValueService typeValueService)
        {
            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)
            
            // this.expAnalyzer = new QsExpAnalyzer(typeValueFactory);
            this.stmtAnalyzer = new QsStmtAnalyzer(this, typeValueService);
            this.capturer = new QsCapturer();
            this.typeValueService = new QsTypeValueService();
        }

        //internal QsTypeValue? AnalyzeExp(QsExp exp, QsAnalyzerContext context)
        //{
        //    return expAnalyzer.AnalyzeExp(exp, context);
        //}

        internal bool CaptureStmt(QsStmt stmt, ref QsCaptureContext captureContext)
        {
            var captureResult = capturer.CaptureStmt(stmt, captureContext);
            if (captureResult.HasValue)
            {
                captureContext = captureResult.Value;
                return true;
            }

            return false;
        }

        internal void AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        {
            // 1. int x  // x를 추가
            // 2. int x = initExp // x 추가, initExp가 int인지 검사
            // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가

            // TODO: 추후에는 매번 만들지 않고, QsAnalyzerContext안에서 직접 관리한다
            var declTypeValue = context.TypeValuesByTypeExp[varDecl.Type];

            foreach (var elem in varDecl.Elements)
            {
                if (elem.InitExp == null)
                {
                    if (declTypeValue is QsVarTypeValue)
                        context.Errors.Add((elem, $"{elem.VarName}의 타입을 추론할 수 없습니다"));
                    else
                        context.AddVarType(elem.VarName, declTypeValue);
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

                        if (!IsAssignable(declTypeValue, initExpTypeValue))
                            context.Errors.Add((elem, $"타입 {initExpTypeValue}의 값은 타입 {varDecl.Type}의 변수 {elem.VarName}에 대입할 수 없습니다."));
                    }

                    if (context.bGlobalScope)
                        context.AddGlobalVarType(elem.VarName, declTypeValue);
                    else
                        context.AddVarType(elem.VarName, declTypeValue);
                }
            }
        }

        public bool AnalyzeExp(QsExp exp, QsAnalyzerContext context, [NotNullWhen(returnValue:true)] out QsTypeValue? typeValue)
        {
            throw new NotImplementedException();
        }

        public void AnalyzeStmt(QsStmt stmt, QsAnalyzerContext context)
        {
            stmtAnalyzer.AnalyzeStmt(stmt, context);
        }

        public static QsAnalyzerContext? AnalyzeScript(QsScript script)
        {
            var typeIdFactory = new QsTypeIdFactory();
            var funcIdFactory = new QsFuncIdFactory();

            var typeSkeletonCollector = new QsTypeSkeletonCollector(typeIdFactory, funcIdFactory);

            var typeExpEvaluator = new QsTypeExpEvaluator();            
            
            var typeBuilder = new QsTypeAndFuncBuilder();

            var capturer = new QsCapturer();
            var typeValueService = new QsTypeValueService();
            var analyzer = new QsAnalyzer(capturer, typeValueService);

            var errors = new List<(object obj, string msg)>();

            var refSkeletons = new[] {
                new QsTypeSkeleton(typeIdFactory.MakeTypeId(), "void", 0),
                new QsTypeSkeleton(typeIdFactory.MakeTypeId(), "bool", 0),
                new QsTypeSkeleton(typeIdFactory.MakeTypeId(), "int", 0),
                new QsTypeSkeleton(typeIdFactory.MakeTypeId(), "string", 0),
                new QsTypeSkeleton(typeIdFactory.MakeTypeId(), "List", 1),
            };

            // 1. type skeleton 모으기
            var skeletonCollectorContext = new QsTypeSkeletonCollectorContext(refSkeletons);
            if (!typeSkeletonCollector.CollectScript(script, skeletonCollectorContext))
            {
                errors.Add((script, $"타입 정보 모으기에 실패했습니다"));
                return null;
            }

            // 2. skeleton으로 트리의 모든 TypeExp들을 TypeValue로 변환하기
            var typeExpEvaluatorContext = new QsTypeEvalContext(
                skeletonCollectorContext.TypeIdsByLocation.ToImmutableDictionary(),
                skeletonCollectorContext.FuncIdsByLocation.ToImmutableDictionary(),
                skeletonCollectorContext.TypeSkeletonsByTypeId.ToImmutableDictionary(),
                skeletonCollectorContext.GlobalTypeSkeletons.ToImmutableDictionary());
            typeExpEvaluator.EvaluateScript(script, typeExpEvaluatorContext);

            errors.AddRange(typeExpEvaluatorContext.Errors);

            var typeValuesByTypeExp = typeExpEvaluatorContext.TypeValuesByTypeExp.ToImmutableDictionary();

            // 3. Type, Func만들기
            var builderContext = new QsTypeAndFuncBuilderContext(
                skeletonCollectorContext.TypeIdsByLocation.ToImmutableDictionary(),
                skeletonCollectorContext.FuncIdsByLocation.ToImmutableDictionary(),
                typeValuesByTypeExp);
            typeBuilder.BuildScript(script, builderContext);

            var globalTypes = builderContext.GlobalTypes.ToImmutableDictionary(type => type.GetName());
            var typesById = builderContext.Types.ToImmutableDictionary(type => type.TypeId);
            var funcsById = builderContext.Funcs.ToImmutableDictionary(type => type.FuncId);

            var boolValueType = new QsNormalTypeValue(null, globalTypes["bool"].TypeId);
            var voidValueType = new QsNormalTypeValue(null, globalTypes["void"].TypeId);

            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            var analyzerContext = new QsAnalyzerContext(
                typesById,
                funcsById,
                new Dictionary<QsTypeExp, QsTypeValue>(typeExpEvaluatorContext.TypeValuesByTypeExp, QsReferenceComparer<QsTypeExp>.Instance), 
                globalTypes,
                boolValueType,
                voidValueType);
            analyzer.AnalyzeScript(script, analyzerContext);

            return analyzerContext;
        }

        public void AnalyzeScript(QsScript script, QsAnalyzerContext context)
        {   
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
                    case QsFuncDeclScriptElement funcElem: break;
                }
            }
        }
    }
}
