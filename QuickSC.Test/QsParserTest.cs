using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsParserTest
    {
        //[Fact]
        //public async ValueTask TestSimpleCaseAsync()
        //{
        //    var parser = new QsParser();
        //    var script = await parser.ParseScriptAsync(new QsBuffer(new StringReader("ls -al")).MakePosition());

        //}

        async ValueTask<QsParserContext> MakeContextAsync(string input)        
        {
            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            return QsParserContext.Make(lexerContext);
        }

        [Fact]
        public async Task TestParseIdentifierExpAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync("x");
            var expResult = await parser.ParseExpAsync(context);

            Assert.Equal(new QsIdentifierExp("x"), expResult.Elem);
        }

        [Fact]
        public async Task TestParseStringExpAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync("\"aaa bbb ${\"xxx ${ddd}\"} ddd\"");
            var expResult = await parser.ParseExpAsync(context);

            var expected = new QsStringExp(new List<QsStringExpElement> 
            {
                new QsTextStringExpElement("aaa bbb "),
                new QsExpStringExpElement(new QsStringExp(new List<QsStringExpElement>
                {
                    new QsTextStringExpElement("xxx "),
                    new QsExpStringExpElement(new QsIdentifierExp("ddd"))
                })),
                new QsTextStringExpElement(" ddd")
            });

            Assert.Equal(expected, expResult.Elem);
        }
    }
}
