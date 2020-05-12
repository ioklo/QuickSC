﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using QuickSC.Syntax;
using QuickSC.TypeExpEvaluator;
using static QuickSC.StaticAnalyzer.QsAnalyzerExtension;

namespace QuickSC.StaticAnalyzer
{
    public class QsAnalyzer
    {
        QsTypeIdFactory typeIdFactory;
        QsTypeSkeletonCollector typeSkeletonCollector;
        QsTypeExpEvaluator typeExpEvaluator;
        // QsExpAnalyzer expAnalyzer;
        // QsStmtAnalyzer stmtAnalyzer;        

        public QsAnalyzer()
        {
            // 내부 전용 클래스는 new를 써서 직접 만들어도 된다 (DI, 인자로 받을 필요 없이)

            this.typeIdFactory = new QsTypeIdFactory();
            this.typeSkeletonCollector = new QsTypeSkeletonCollector(typeIdFactory);
            this.typeExpEvaluator = new QsTypeExpEvaluator();

            // this.expAnalyzer = new QsExpAnalyzer(typeValueFactory);
            // this.stmtAnalyzer = new QsStmtAnalyzer(this);
        }

        //internal QsTypeValue? AnalyzeExp(QsExp exp, QsAnalyzerContext context)
        //{
        //    return expAnalyzer.AnalyzeExp(exp, context);
        //}

        //internal void AnalyzeVarDecl(QsVarDecl varDecl, QsAnalyzerContext context)
        //{
        //    // 1. int x  // x를 추가
        //    // 2. int x = initExp // x 추가, initExp가 int인지 검사
        //    // 3. var x = initExp // initExp의 타입을 알아내고 x를 추가

        //    // TODO: 추후에는 매번 만들지 않고, QsAnalyzerContext안에서 직접 관리한다
        //    var declTypeValue = context.TypeExpTypeValues[varDecl.Type];

        //    foreach (var elem in varDecl.Elements)
        //    {
        //        if (elem.InitExp == null)
        //        {
        //            if (declTypeValue is QsVarTypeValue)
        //                context.AddError(elem, $"{elem.VarName}의 타입을 추론할 수 없습니다");
        //            else 
        //                context.AddVarType(elem.VarName, declTypeValue);
        //        }
        //        else
        //        {
        //            var initExpTypeValue = AnalyzeExp(elem.InitExp, context);
        //            if (initExpTypeValue == null) return;

        //            // var 처리
        //            if (declTypeValue is QsVarTypeValue)
        //                context.AddVarType(elem.VarName, initExpTypeValue);
        //            else if (!IsAssignable(declTypeValue, initExpTypeValue))
        //                context.AddError(elem, $"타입 {initExpTypeValue}의 값은 타입 {varDecl.Type}의 변수 {elem.VarName}에 대입할 수 없습니다.");
        //            else
        //                context.AddVarType(elem.VarName, declTypeValue);
        //        }
        //    }
        //}

        public void AnalyzeScript(QsScript script, QsAnalyzerContext context)
        {
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
                context.AddError(script, $"타입 정보 모으기에 실패했습니다");
                return;
            }

            // 2. skeleton으로 트리의 모든 TypeExp들을 TypeValue로 변환하기
            var typeExpEvaluatorContext = new QsTypeEvalContext(
                skeletonCollectorContext.TypeSkeletonsByTypeId.ToImmutableDictionary(),
                skeletonCollectorContext.GlobalTypeSkeletons.ToImmutableDictionary(),
                skeletonCollectorContext.TypeIdsByTypeDecl.ToImmutableDictionary());
            typeExpEvaluator.EvaluateScript(script, typeExpEvaluatorContext);

            context.Errors.AddRange(typeExpEvaluatorContext.Errors);

            // 3. enum등의 type 세부 정보 만들기
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsEnumDeclScriptElement enumElem:
                        // typeValueFactory.AddGlobalEnum(enumElem.EnumDecl);
                        break;
                }
            }

            // 3. func 시그니처를 모은다, type에 딸린 함수도 여기서 모은다 (1의 타입정보들이 필요하다)
            foreach (var elem in script.Elements)
            {
                switch (elem)
                {
                    case QsFuncDeclScriptElement funcElem: break;
                }
            }

            // 4. stmt를 분석한다 (3의 함수정보가 필요하다)
            foreach (var elem in script.Elements)
            {
                switch(elem)
                {                    
                    case QsStmtScriptElement stmtElem: break;
                }
            }

            // 5. 각 func body를 분석한다 (4에서 얻게되는 글로벌 변수 정보가 필요하다)
            foreach(var elem in script.Elements)
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
