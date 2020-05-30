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

        public QsDefaultApplication(IQsCommandProvider commandProvider)
        {
            QsLexer lexer = new QsLexer();
            parser = new QsParser(lexer);

            typeSkeletonCollector = new QsTypeSkeletonCollector();
            typeExpEvaluator = new QsTypeExpEvaluator();
            typeBuilder = new QsTypeAndFuncBuilder();

            var capturer = new QsCapturer();
            analyzer = new QsAnalyzer(capturer);
            
            evaluator = new QsEvaluator(commandProvider);        
        }
        
        public async ValueTask<bool> RunAsync(
            string moduleName, string input, IQsRuntimeModule runtimeModule, ImmutableArray<IQsModule> modulesExceptRuntimeModule, IQsErrorCollector errorCollector) // 레퍼런스를 포함
        {
            var metadatasBuilder = ImmutableArray.CreateBuilder<IQsMetadata>(modulesExceptRuntimeModule.Length + 1);

            metadatasBuilder.Add((IQsMetadata)runtimeModule);
            foreach(var module in modulesExceptRuntimeModule)
                metadatasBuilder.Add((IQsMetadata)module);

            var metadatas = metadatasBuilder.MoveToImmutable();

            // 파싱 QsParserContext -> QsScript             
            var script = await parser.ParseScriptAsync(input);
            if (script == null)
                return false;

            if (!typeSkeletonCollector.CollectScript(moduleName, script, errorCollector, out var collectInfo))
                return false;

            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            if (!typeExpEvaluator.EvaluateScript(script, metadatas, collectInfo, errorCollector, out var typeEvalInfo))
                return false;

            // 3. Type, Func만들기, MetadataBuilder
            var buildInfo = typeBuilder.BuildScript(moduleName, script, typeEvalInfo);

            // globalVariable이 빠진상태            
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            if (!analyzer.AnalyzeScript(moduleName, script, metadatas, buildInfo, errorCollector, out var analyzeInfo))
                return false;

            var scriptModule = new QsScriptModule(moduleName);
            var domainService = new QsDomainService(runtimeModule, modulesExceptRuntimeModule.Add(scriptModule));

            if (!await evaluator.EvaluateScriptAsync(script, runtimeModule, domainService, analyzeInfo))
                return false;

            return true;
        }
    }

}
