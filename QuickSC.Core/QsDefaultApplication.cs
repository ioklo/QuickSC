using QuickSC.Runtime;
using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    public class QsDefaultApplication
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
        
        public async ValueTask<int?> RunAsync(
            string moduleName, string input, IQsRuntimeModule runtimeModule, ImmutableArray<IQsModule> modulesExceptRuntimeModule, IQsErrorCollector errorCollector) // 레퍼런스를 포함
        {
            var metadatasBuilder = ImmutableArray.CreateBuilder<IQsMetadata>(modulesExceptRuntimeModule.Length + 1);

            metadatasBuilder.Add(runtimeModule);
            foreach(var module in modulesExceptRuntimeModule)
                metadatasBuilder.Add(module);

            var metadatas = metadatasBuilder.MoveToImmutable();

            // 파싱 QsParserContext -> QsScript             
            var script = await parser.ParseScriptAsync(input);
            if (script == null)
                return null;

            if (!typeSkeletonCollector.CollectScript(script, errorCollector, out var skelResult))
                return null;

            var typeEvalMetadataService = new QsMetadataService(metadatas);

            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            if (!typeExpEvaluator.EvaluateScript(script, typeEvalMetadataService, skelResult, errorCollector, out var typeEvalResult))
                return null;

            // 3. Type, Func만들기, MetadataBuilder
            var typeAndFuncBuildResult = typeBuilder.BuildScript(moduleName, script, skelResult, typeEvalResult);

            var scriptMetadata = new QsScriptMetadata(
                moduleName, 
                typeAndFuncBuildResult.Types,
                typeAndFuncBuildResult.Funcs,
                typeAndFuncBuildResult.Vars);

            var metadataService = new QsMetadataService(metadatas.Append(scriptMetadata));
            var typeValueApplier = new QsTypeValueApplier(metadataService);
            var typeValueService = new QsTypeValueService(metadataService, typeValueApplier);

            // globalVariable이 빠진상태            
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            if (!analyzer.AnalyzeScript(script, metadataService, typeValueService, typeEvalResult, typeAndFuncBuildResult, errorCollector, out var analyzeInfo))
                return null;

            var scriptModule = new QsScriptModule(
                scriptMetadata,
                analyzeInfo.FuncTemplatesById);

            var domainService = new QsDomainService();
            var staticValueService = new QsStaticValueService();

            domainService.LoadModule(runtimeModule);
            domainService.LoadModule(scriptModule);

            return await evaluator.EvaluateScriptAsync(script, runtimeModule, domainService, typeValueService, staticValueService, analyzeInfo);
        }
    }

}
