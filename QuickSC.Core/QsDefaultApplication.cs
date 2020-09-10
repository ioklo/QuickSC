using Gum;
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
        Parser parser;
        QsEvaluator evaluator;

        public QsDefaultApplication(IQsCommandProvider commandProvider)
        {
            Lexer lexer = new Lexer();
            parser = new Parser(lexer);

            var typeSkeletonCollector = new QsTypeSkeletonCollector();
            var typeExpEvaluator = new QsTypeExpEvaluator(typeSkeletonCollector);
            var typeAndFuncBuilder = new QsMetadataBuilder(typeExpEvaluator);

            var capturer = new QsCapturer();
            var analyzer = new QsAnalyzer(typeAndFuncBuilder, capturer);
            
            evaluator = new QsEvaluator(analyzer, commandProvider);        
        }
        
        public async ValueTask<int?> RunAsync(
            string moduleName, string input, IQsRuntimeModule runtimeModule, ImmutableArray<IQsModule> modulesExceptRuntimeModule, IQsErrorCollector errorCollector) // 레퍼런스를 포함
        {
            var metadatas = new List<IQsMetadata>(modulesExceptRuntimeModule.Length + 1);

            metadatas.Add(runtimeModule);
            foreach(var module in modulesExceptRuntimeModule)
                metadatas.Add(module);

            // 파싱 QsParserContext -> QsScript             
            var script = await parser.ParseScriptAsync(input);
            if (script == null)
                return null;
            
            return await evaluator.EvaluateScriptAsync(moduleName, script, runtimeModule, metadatas, errorCollector);
        }
    }

}
