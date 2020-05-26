using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    class QsDefaultApplication
    {
        QsParser parser;
        QsTypeSkeletonCollector typeSkeletonCollector;
        QsTypeExpEvaluator typeExpEvaluator;
        QsTypeAndFuncBuilder typeBuilder;
        QsAnalyzer analyzer;
        QsEvaluator evaluator;

        public QsDefaultApplication(IQsCommandProvider commandProvider, IQsRuntimeModule runtimeModule)
        {
            QsLexer lexer = new QsLexer();
            parser = new QsParser(lexer);

            typeSkeletonCollector = new QsTypeSkeletonCollector();
            typeExpEvaluator = new QsTypeExpEvaluator();
            typeBuilder = new QsTypeAndFuncBuilder();

            var capturer = new QsCapturer();
            var typeValueService = new QsTypeValueService();
            analyzer = new QsAnalyzer(capturer, typeValueService);

            var domainService = new QsDomainService();
            evaluator = new QsEvaluator(domainService, commandProvider, runtimeModule);
        }

        // 
        public async ValueTask<bool> RunAsync(string input, ImmutableArray<IQsModule> modules, IQsErrorCollector errorCollector) // 레퍼런스를 포함
        {
            var metadatas = ImmutableArray.CreateRange(modules, module => (IQsMetadata)module);

            // 파싱 QsParserContext -> QsScript             
            var script = await parser.ParseScriptAsync(input);
            if (script == null)
                return false;

            if (!typeSkeletonCollector.CollectScript(script, errorCollector, out var collectInfo))
                return false;

            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            if (!typeExpEvaluator.EvaluateScript(script, metadatas, collectInfo, errorCollector, out var typeEvalInfo))
                return false;

            // 3. Type, Func만들기, MetadataBuilder
            var buildInfo = typeBuilder.BuildScript(script, typeEvalInfo);

            // globalVariable이 빠진상태            
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            if (!analyzer.AnalyzeScript(script, buildInfo, errorCollector, out var analyzeInfo))
                return false;           
            

            return analyzerContext;

            // 분석 QsScript, references -> 
            var analyzerContext = new QsAnalyzerContext()
            analyzer.AnalyzeScript(scriptResult.Elem, analyzerContext);

            // 실행 QsScript, QsAnalyzeResult -> 실제 실행
            // var evalContext = QsEvalContext.Make(analyzerContext);
            // await evaluator.EvaluateScriptAsync(scriptResult.Elem, evalContext);
            // var evaluator = new QsEvaluator(analyzerContext, )
        }
    }

}
