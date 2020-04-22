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
    public class QsStmtParserTest
    {
        async ValueTask<QsParserContext> MakeContextAsync(string input)        
        {
            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            return QsParserContext.Make(lexerContext);
        }

        [Fact]
        async Task TestParseInlineCommandStmtAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsStmtParser(lexer);
            var context = (await MakeContextAsync("@echo ${a}bbb  ")).AddType("string");

            var cmdStmt = await parser.ParseCommandStmtAsync(context);

            var expected = new QsCommandStmt(
                new QsStringExp(
                    new QsTextStringExpElement("echo "),
                    new QsExpStringExpElement(new QsIdentifierExp("a")),
                    new QsTextStringExpElement("bbb  ")));

            Assert.Equal(expected, cmdStmt.Elem);
        }

        [Fact]
        async Task TestParseBlockCommandStmtAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsStmtParser(lexer);

            var input = @"
@{ 
    echo ${ a } bbb   
xxx
}
";
            var context = (await MakeContextAsync(input)).AddType("string");

            var cmdStmt = await parser.ParseCommandStmtAsync(context);

            var expected = new QsCommandStmt(
                new QsStringExp(
                    new QsTextStringExpElement("    echo "),
                    new QsExpStringExpElement(new QsIdentifierExp("a")),
                    new QsTextStringExpElement(" bbb   ")),
                new QsStringExp(new QsTextStringExpElement("xxx")));

            Assert.Equal(expected, cmdStmt.Elem);
        }

        [Fact]
        async Task TestParseVarDeclStmtAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsStmtParser(lexer);
            var context = (await MakeContextAsync("string a = \"hello\";")).AddType("string");

            var varDeclStmt = await parser.ParseVarDeclStmtAsync(context);

            var expected = new QsVarDeclStmt(new QsVarDecl("string",
                new QsVarDeclElement("a", new QsStringExp(new QsTextStringExpElement("hello")))));

            Assert.Equal(expected, varDeclStmt.Elem);
        }


        [Fact]
        async Task TestParseIfStmtAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsStmtParser(lexer);
            var context = (await MakeContextAsync("if (b) {} else if (c) {} else {}")).AddType("string");

            var ifStmt = await parser.ParseIfStmtAsync(context);

            var expected = new QsIfStmt(new QsIdentifierExp("b"),
                new QsBlockStmt(ImmutableArray<QsStmt>.Empty),
                new QsIfStmt(new QsIdentifierExp("c"),
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty),
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty)));

            Assert.Equal(expected, ifStmt.Elem);
        }

        [Fact]
        Task TestParseForStmtAsync()
        {
            throw new NotImplementedException();
        }

        [Fact]
        Task TestParseContinueStmtAsync()
        {
            throw new NotImplementedException();
        }

        [Fact]
        Task TestParseBreakStmtAsync()
        {
            throw new NotImplementedException();
        }

        [Fact]
        Task TestParseBlockStmtAsync()
        {
            throw new NotImplementedException();
        }

        [Fact]
        Task TestParseExpStmtAsync()
        {
            throw new NotImplementedException();
        }
    }
}
