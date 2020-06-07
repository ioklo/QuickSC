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

            if (!typeSkeletonCollector.CollectScript(moduleName, script, errorCollector, out var skelResult))
                return false;

            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            if (!typeExpEvaluator.EvaluateScript(script, metadatas, skelResult, errorCollector, out var typeEvalResult))
                return false;

            // 3. Type, Func만들기, MetadataBuilder
            var typeAndFuncBuildResult = typeBuilder.BuildScript(moduleName, script, skelResult, typeEvalResult);

            var metadataService = new QsMetadataService(
                moduleName,
                typeAndFuncBuildResult.Types.ToImmutableDictionary(type => type.TypeId),
                typeAndFuncBuildResult.Funcs.ToImmutableDictionary(func => func.FuncId),
                typeAndFuncBuildResult.Vars.ToImmutableDictionary(v => v.VarId),
                metadatas);

            // globalVariable이 빠진상태            
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            if (!analyzer.AnalyzeScript(moduleName, script, metadataService, typeEvalResult, typeAndFuncBuildResult, errorCollector, out var analyzeInfo))
                return false;

            var scriptModule = new QsScriptModule(moduleName, analyzeInfo.FuncTemplatesById);

            var domainService = new QsDomainService(metadataService, runtimeModule, modulesExceptRuntimeModule.Add(scriptModule));

            var staticValueService = new QsStaticValueService();

            if (!await evaluator.EvaluateScriptAsync(script, runtimeModule, domainService, staticValueService, analyzeInfo))
                return false;

            return true;
        }
    }

}
