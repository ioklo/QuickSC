using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class QsParserTest
    {
        async ValueTask<QsParserContext> MakeContextAsync(string input)        
        {
            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            return QsParserContext.Make(lexerContext);
        }

        [Fact]
        public async Task TestParseScriptAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync("ls -al");
            var script = await parser.ParseScriptAsync(context);

            var expected = new QsScript(ImmutableArray.Create<QsScriptElement>(
                new QsStmtScriptElement(new QsCommandStmt(
                    new QsStringExp(ImmutableArray.Create<QsStringExpElement>(new QsTextStringExpElement("ls"))),
                    ImmutableArray.Create<QsExp>(
                        new QsStringExp(ImmutableArray.Create<QsStringExpElement>(new QsTextStringExpElement("-al")))
                    )))
            ));

            Assert.Equal(expected, script.Elem);
        }

        [Fact] async Task TestParseVarDeclStmtAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = (await MakeContextAsync("string a = \"hello\";")).AddType("string");

            var varDeclStmt = await parser.ParseVarDeclStmtAsync(context);

            var expected = new QsVarDeclStmt("string", ImmutableArray.Create<QsVarDeclStmtElement>(
                new QsVarDeclStmtElement("a", new QsStringExp(ImmutableArray.Create<QsStringExpElement>(
                    new QsTextStringExpElement("hello"))
                ))
            ));

            Assert.Equal(expected, varDeclStmt.Elem);
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

            var expected = new QsStringExp(ImmutableArray.Create<QsStringExpElement>(
                new QsTextStringExpElement("aaa bbb "),
                new QsExpStringExpElement(new QsStringExp(ImmutableArray.Create<QsStringExpElement>(
                    new QsTextStringExpElement("xxx "),
                    new QsExpStringExpElement(new QsIdentifierExp("ddd"))
                ))),
                new QsTextStringExpElement(" ddd")
            ));

            Assert.Equal(expected, expResult.Elem);
        }
    }
}
