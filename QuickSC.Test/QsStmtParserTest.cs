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
        async ValueTask<(QsStmtParser, QsParserContext)> PrepareAsync(string input)
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);

            var buffer = new QsBuffer(new StringReader(input));
            var bufferPos = await buffer.MakePosition().NextAsync();
            var lexerContext = QsLexerContext.Make(bufferPos);
            var context = QsParserContext.Make(lexerContext);

            return (parser.stmtParser, context);
        }

        [Fact]
        async Task TestParseInlineCommandStmtAsync()
        {
            (var parser, var context) = await PrepareAsync("@echo ${a}bbb  ");
            
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
            var input = @"
@{ 
    echo ${ a } bbb   
xxx
}
";          
            (var parser, var context) = await PrepareAsync(input);

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
            (var parser, var context) = await PrepareAsync("string a = \"hello\";");
            
            var varDeclStmt = await parser.ParseVarDeclStmtAsync(context);

            var expected = new QsVarDeclStmt(new QsVarDecl(new QsIdTypeExp("string"),
                new QsVarDeclElement("a", new QsStringExp(new QsTextStringExpElement("hello")))));

            Assert.Equal(expected, varDeclStmt.Elem);
        }
        
        [Fact]
        async Task TestParseIfStmtAsync()
        {
            (var parser, var context) = await PrepareAsync("if (b) {} else if (c) {} else {}");
            
            var ifStmt = await parser.ParseIfStmtAsync(context);

            var expected = new QsIfStmt(new QsIdentifierExp("b"),
                null,
                new QsBlockStmt(ImmutableArray<QsStmt>.Empty),                
                new QsIfStmt(
                    new QsIdentifierExp("c"),
                    null,
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty),
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty)));

            Assert.Equal(expected, ifStmt.Elem);
        }

        [Fact]
        async Task TestParseIfStmtWithTestTypeAsync()
        {
            (var parser, var context) = await PrepareAsync("if (b is T) {} else if (c) {} else {}");

            var ifStmt = await parser.ParseIfStmtAsync(context);

            var expected = new QsIfStmt(new QsIdentifierExp("b"),
                new QsIdTypeExp("T"),
                new QsBlockStmt(ImmutableArray<QsStmt>.Empty),
                new QsIfStmt(
                    new QsIdentifierExp("c"),
                    null,
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty),
                    new QsBlockStmt(ImmutableArray<QsStmt>.Empty)));

            Assert.Equal(expected, ifStmt.Elem);
        }

        [Fact]
        async Task TestParseForStmtAsync()
        {
            (var parser, var context) = await PrepareAsync(@"
for (f(); g; h + g) ;
");

            var result = await parser.ParseForStmtAsync(context);

            var expected = new QsForStmt(
                new QsExpForStmtInitializer(new QsCallExp(new QsIdentifierExp("f"))),
                new QsIdentifierExp("g"),
                new QsBinaryOpExp(QsBinaryOpKind.Add, new QsIdentifierExp("h"), new QsIdentifierExp("g")),
                QsBlankStmt.Instance);

            Assert.Equal(expected, result.Elem);
        }

        [Fact]
        async Task TestParseContinueStmtAsync()
        {
            (var parser, var context) = await PrepareAsync(@"continue;");
            var continueResult = await parser.ParseContinueStmtAsync(context);

            Assert.Equal(QsContinueStmt.Instance, continueResult.Elem);
        }

        [Fact]
        async Task TestParseBreakStmtAsync()
        {
            (var parser, var context) = await PrepareAsync(@"break;");
            var breakResult = await parser.ParseBreakStmtAsync(context);

            Assert.Equal(QsBreakStmt.Instance, breakResult.Elem);
        }

        [Fact]
        async Task TestParseBlockStmtAsync()
        {
            (var parser, var context) = await PrepareAsync(@"{ { } { ; } ; }");
            var blockResult = await parser.ParseBlockStmtAsync(context);

            var expected = new QsBlockStmt(
                new QsBlockStmt(),
                new QsBlockStmt(QsBlankStmt.Instance),
                QsBlankStmt.Instance);

            Assert.Equal(expected, blockResult.Elem);
        }

        [Fact]
        async Task TestParseBlankStmtAsync()
        {
            (var parser, var context) = await PrepareAsync("  ;  ");
            var blankResult = await parser.ParseBlankStmtAsync(context);

            Assert.Equal(QsBlankStmt.Instance, blankResult.Elem);
        }

        [Fact]
        async Task TestParseExpStmtAsync()
        {
            (var parser, var context) = await PrepareAsync("a = b * c(1);");
            var expResult = await parser.ParseExpStmtAsync(context);

            var expected = new QsExpStmt(new QsBinaryOpExp(QsBinaryOpKind.Assign,
                new QsIdentifierExp("a"),
                new QsBinaryOpExp(QsBinaryOpKind.Multiply,
                    new QsIdentifierExp("b"),
                    new QsCallExp(new QsIdentifierExp("c"), new QsIntLiteralExp(1)))));
                

            Assert.Equal(expected, expResult.Elem);
        }

        [Fact]
        async Task TestParseForeachStmtAsync()
        {
            var (parser, context) = await PrepareAsync("foreach( var x in l ) { } ");
            var stmtResult = await parser.ParseForeachStmtAsync(context);

            var expected = new QsForeachStmt(new QsIdTypeExp("var"), "x", new QsIdentifierExp("l"), new QsBlockStmt());

            Assert.Equal(expected, stmtResult.Elem);
        }
    }
}
