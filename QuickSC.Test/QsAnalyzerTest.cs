using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Collections;
using System.IO;
using System.IO.Enumeration;
using QuickSC.Syntax;
using QuickSC.TypeExpEvaluator;

namespace QuickSC
{
    public class QsAnalyzerTest
    {
        //[Fact]
        //void TestAnalyzeIntLiteralExp()
        //{
        //    var typeValueFactory = new QsTypeValueFactory();
        //    var intTypeValue = typeValueFactory.GetTypeValue("int");

        //    var expAnalyzer = new QsExpAnalyzer(typeValueFactory);
        //    var context = new QsAnalyzerContext();

        //    var result = expAnalyzer.AnalyzeIntLiteralExp(new Syntax.QsIntLiteralExp(3), context);

        //    Assert.False(context.HasError());
        //    Assert.Equal(result, intTypeValue);
        //}

        [Theory]
        [MemberData(nameof(GetScriptData))]
        public async Task TestAnalyzeScript(string file)
        {
            QsParseResult<QsScript> scriptResult;

            using (var streamReader = new StreamReader(file))
            {
                var buffer = new QsBuffer(streamReader);
                var pos = await buffer.MakePosition().NextAsync();

                var lexer = new QsLexer();
                var parser = new QsParser(lexer);
                var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));
                scriptResult = await parser.ParseScriptAsync(parserContext);
            }

            Assert.True(scriptResult.HasValue);

            var analyzer = new QsAnalyzer();
            var context = new QsAnalyzerContext();

            analyzer.AnalyzeScript(scriptResult.Elem, context);

            // 통과만 하는 시나리오
            Assert.False(context.HasError());
        }

        public static IEnumerable<object[]> GetScriptData()
        {
            foreach (var file in Directory.EnumerateFiles("Input", "*.qs"))
            {
                yield return new object[] { file };
            }
        }
    }

    public class TestData
    {
        public string DisplayName { get; }
        public string input { get; }
        public TestData(string displayName, string text)
        {
            DisplayName = displayName;
            input = text;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
    
}
