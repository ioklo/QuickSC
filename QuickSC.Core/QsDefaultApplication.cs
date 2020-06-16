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
            string moduleName, string input, IQsRuntimeModuleInfo runtimeModuleInfo, ImmutableArray<IQsModule> modulesExceptRuntimeModule, IQsErrorCollector errorCollector) // 레퍼런스를 포함
        {
            var metadatasBuilder = ImmutableArray.CreateBuilder<IQsMetadata>(modulesExceptRuntimeModule.Length + 1);

            metadatasBuilder.Add(runtimeModuleInfo.GetMetadata());
            foreach(var module in modulesExceptRuntimeModule)
                metadatasBuilder.Add((IQsMetadata)module);

            var metadatas = metadatasBuilder.MoveToImmutable();

            // 파싱 QsParserContext -> QsScript             
            var script = await parser.ParseScriptAsync(input);
            if (script == null)
                return null;

            if (!typeSkeletonCollector.CollectScript(script, errorCollector, out var skelResult))
                return null;

            var typeEvalMetadataService = new QsMetadataService(
                ImmutableArray<QsType>.Empty,
                ImmutableArray<QsFunc>.Empty,
                ImmutableArray<QsVariable>.Empty,
                metadatas);

            // 2. skeleton과 metadata로 트리의 모든 TypeExp들을 TypeValue로 변환하기            
            if (!typeExpEvaluator.EvaluateScript(script, typeEvalMetadataService, skelResult, errorCollector, out var typeEvalResult))
                return null;

            // 3. Type, Func만들기, MetadataBuilder
            var typeAndFuncBuildResult = typeBuilder.BuildScript(moduleName, script, skelResult, typeEvalResult);

            var metadataService = new QsMetadataService(
                typeAndFuncBuildResult.Types,
                typeAndFuncBuildResult.Funcs,
                typeAndFuncBuildResult.Vars,
                metadatas);

            // globalVariable이 빠진상태            
            // 4. stmt를 분석하고, 전역 변수 타입 목록을 만든다 (3의 함수정보가 필요하다)
            if (!analyzer.AnalyzeScript(moduleName, script, metadataService, typeEvalResult, typeAndFuncBuildResult, errorCollector, out var analyzeInfo))
                return null;

            var domainService = new QsDomainService(metadataService);
            var staticValueService = new QsStaticValueService();

            var runtimeModule = runtimeModuleInfo.MakeRuntimeModule();

            domainService.LoadModule(runtimeModule);

            // 스크립트는 수동 로드
            domainService.AddFuncInfos(analyzeInfo.FuncTemplatesById.Select( kv =>
            {
                if (kv.Value is QsScriptFuncTemplate.FuncDecl funcDecl)
                    return new QsScriptModuleFuncInfo(kv.Key, funcDecl);
                else
                    throw new NotImplementedException();
            }));

            return await evaluator.EvaluateScriptAsync(script, runtimeModule, domainService, staticValueService, analyzeInfo);
        }
    }

}
