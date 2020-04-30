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
        public async Task TestParseSimpleScriptAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync("@ls -al");
            var script = await parser.ParseScriptAsync(context);

            var expected = new QsScript(new QsStmtScriptElement(new QsCommandStmt(new QsStringExp(new QsTextStringExpElement("ls -al")))));

            Assert.Equal(expected, script.Elem);
        }

        [Fact]
        public async Task TestParseFuncDeclAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync("void Func(int x, params string y, int z) { int a = 0; }");
            var funcDecl = await parser.ParseFuncDeclAsync(context);

            var expected = new QsFuncDecl(
                new QsTypeIdExp("void"),
                "Func", 1,
                new QsBlockStmt(new QsVarDeclStmt(new QsVarDecl("int", new QsVarDeclElement("a", new QsIntLiteralExp(0))))),
                new QsFuncDeclParam(new QsTypeIdExp("int"), "x"),
                new QsFuncDeclParam(new QsTypeIdExp("string"), "y"),
                new QsFuncDeclParam(new QsTypeIdExp("int"), "z"));

            Assert.Equal(expected, funcDecl.Elem);
        }

        [Fact]
        public async Task TestParseComplexScriptAsync()
        {
            var lexer = new QsLexer();
            var parser = new QsParser(lexer);
            var context = await MakeContextAsync(@"
int sum = 0;

for (int i = 0; i < 5; i++)
{
    if (i % 2 == 0)
        sum = sum + i;
    else @{ 
        echo hi 
    }
}

@echo $sum Completed!

");
            var script = await parser.ParseScriptAsync(context);

            var expected = new QsScript(
                new QsStmtScriptElement(new QsVarDeclStmt(new QsVarDecl("int", new QsVarDeclElement("sum", new QsIntLiteralExp(0))))),
                new QsStmtScriptElement(new QsForStmt(
                    new QsVarDeclForStmtInitializer(new QsVarDecl("int", new QsVarDeclElement("i", new QsIntLiteralExp(0)))),
                    new QsBinaryOpExp(QsBinaryOpKind.LessThan, new QsIdentifierExp("i"), new QsIntLiteralExp(5)),
                    new QsUnaryOpExp(QsUnaryOpKind.PostfixInc, new QsIdentifierExp("i")),
                    new QsBlockStmt(
                        new QsIfStmt(
                                new QsBinaryOpExp(QsBinaryOpKind.Equal,
                                    new QsBinaryOpExp(QsBinaryOpKind.Modulo, new QsIdentifierExp("i"), new QsIntLiteralExp(2)),
                                    new QsIntLiteralExp(0)),
                                new QsExpStmt(
                                    new QsBinaryOpExp(QsBinaryOpKind.Assign,
                                        new QsIdentifierExp("sum"),
                                        new QsBinaryOpExp(QsBinaryOpKind.Add, new QsIdentifierExp("sum"), new QsIdentifierExp("i")))),
                                new QsCommandStmt(new QsStringExp(new QsTextStringExpElement("        echo hi "))))))),
                new QsStmtScriptElement(new QsCommandStmt(new QsStringExp(
                    new QsTextStringExpElement("echo "),
                    new QsExpStringExpElement(new QsIdentifierExp("sum")),
                    new QsTextStringExpElement(" Completed!")))));
                    
            Assert.Equal(expected, script.Elem);
        }
    }
}
