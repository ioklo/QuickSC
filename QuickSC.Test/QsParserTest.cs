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

            var expected = new QsVarDeclStmt(new QsVarDecl("string", ImmutableArray.Create<QsVarDeclStmtElement>(
                new QsVarDeclStmtElement("a", new QsStringExp(ImmutableArray.Create<QsStringExpElement>(
                    new QsTextStringExpElement("hello"))
                ))
            )));

            Assert.Equal(expected, varDeclStmt.Elem);
        }
    }
}
