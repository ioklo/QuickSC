using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;


namespace QuickSC
{
    public class QsStmtParser
    {
        QsParser parser;
        QsLexer lexer;

        #region Utilities
        bool Accept<TToken>(QsLexResult lexResult, ref QsParserContext context) where TToken : QsToken
        {
            if (lexResult.HasValue && lexResult.Token is TToken)
            {
                context = context.Update(lexResult.Context);
                return true;
            }

            return false;
        }

        bool Accept<TToken>(QsLexResult lexResult, ref QsParserContext context, out TToken? token) where TToken : QsToken
        {
            if (lexResult.HasValue && lexResult.Token is TToken resultToken)
            {
                context = context.Update(lexResult.Context);
                token = resultToken;
                return true;
            }

            token = null;
            return false;
        }

        bool Peek<TToken>(QsLexResult lexResult) where TToken : QsToken
        {
            return lexResult.HasValue && lexResult.Token is TToken;
        }

        bool Parse<TSyntaxElem>(QsParseResult<TSyntaxElem> parseResult, ref QsParserContext context, out TSyntaxElem? elem) where TSyntaxElem : class
        {
            if (!parseResult.HasValue)
            {
                elem = null;
                return false;
            }
            else
            {
                elem = parseResult.Elem;
                context = parseResult.Context;
                return true;
            }
        }


        #endregion

        public QsStmtParser(QsParser parser, QsLexer lexer)
        {
            this.parser = parser;
            this.lexer = lexer;
        }

        internal async ValueTask<QsParseResult<QsIfStmt>> ParseIfStmtAsync(QsParserContext context)
        {
            // if (exp) stmt => If(exp, stmt, null)
            // if (exp) stmt0 else stmt1 => If(exp, stmt0, stmt1)
            // if (exp0) if (exp1) stmt1 else stmt2 => If(exp0, If(exp1, stmt1, stmt2))

            if (!Accept<QsIfToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Parse(await parser.ParseExpAsync(context), ref context, out var cond))            
                return Invalid();

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // right assoc, conflict는 별다른 처리를 하지 않고 지나가면 될 것 같다
            if (!Parse(await ParseStmtAsync(context), ref context, out var body))
                return Invalid();

            QsStmt? elseBody = null;
            if (Accept<QsElseToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (!Parse(await ParseStmtAsync(context), ref context, out elseBody))
                    return Invalid();
            }

            return new QsParseResult<QsIfStmt>(new QsIfStmt(cond!, body!, elseBody), context);

            static QsParseResult<QsIfStmt> Invalid() => QsParseResult<QsIfStmt>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsVarDecl>> ParseVarDeclAsync(QsParserContext context)
        {
            if (!Parse(await parser.ParseTypeExpAsync(context), ref context, out var varType))
                return Invalid();

            var elems = ImmutableArray.CreateBuilder<QsVarDeclElement>();
            do
            {
                if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var varIdResult))
                    return Invalid();

                QsExp? initExp = null;
                if (Accept<QsEqualToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    // TODO: ;나 ,가 나올때까지라는걸 명시해주면 좋겠다
                    if (!Parse(await parser.ParseExpAsync(context), ref context, out initExp))
                        return Invalid();
                }

                elems.Add(new QsVarDeclElement(varIdResult!.Value, initExp));

            } while (Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)); // ,가 나오면 계속한다

            return new QsParseResult<QsVarDecl>(new QsVarDecl(varType!, elems.ToImmutable()), context);

            static QsParseResult<QsVarDecl> Invalid() => QsParseResult<QsVarDecl>.Invalid;
        }

        // int x = 0;
        internal async ValueTask<QsParseResult<QsVarDeclStmt>> ParseVarDeclStmtAsync(QsParserContext context)
        {
            if (!Parse(await ParseVarDeclAsync(context), ref context, out var varDecl))
                return QsParseResult<QsVarDeclStmt>.Invalid;

            if (!context.LexerContext.Pos.IsReachEnd() &&
                !Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)) // ;으로 마무리
                return QsParseResult<QsVarDeclStmt>.Invalid;

            return new QsParseResult<QsVarDeclStmt>(new QsVarDeclStmt(varDecl!), context);
        }

        async ValueTask<QsParseResult<QsForStmtInitializer>> ParseForStmtInitializerAsync(QsParserContext context)
        {
            if (Parse(await ParseVarDeclAsync(context), ref context, out var varDecl))            
                return new QsParseResult<QsForStmtInitializer>(new QsVarDeclForStmtInitializer(varDecl!), context);

            if (Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                return new QsParseResult<QsForStmtInitializer>(new QsExpForStmtInitializer(exp!), context);

            return QsParseResult<QsForStmtInitializer>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsForStmt>> ParseForStmtAsync(QsParserContext context)
        {
            // TODO: Invalid와 Fatal을 구분해야 할 것 같다.. 어찌할지는 깊게 생각을 해보자
            if (!Accept<QsForToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();
            
            // TODO: 이 Initializer의 끝은 ';' 이다
            Parse(await ParseForStmtInitializerAsync(context), ref context, out var initializer);
            
            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // TODO: 이 CondExp의 끝은 ';' 이다            
            Parse(await parser.ParseExpAsync(context), ref context, out var cond);

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();
            
            // TODO: 이 CondExp의 끝은 ')' 이다            
            Parse(await parser.ParseExpAsync(context), ref context, out var cont);
            
            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Parse(await ParseStmtAsync(context), ref context, out var bodyStmt))            
                return Invalid();

            return new QsParseResult<QsForStmt>(new QsForStmt(initializer, cond, cont, bodyStmt!), context);

            static QsParseResult<QsForStmt> Invalid() => QsParseResult<QsForStmt>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsContinueStmt>> ParseContinueStmtAsync(QsParserContext context)
        {
            if (!Accept<QsContinueToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsContinueStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsContinueStmt>.Invalid;

            return new QsParseResult<QsContinueStmt>(QsContinueStmt.Instance, context);
        }

        internal async ValueTask<QsParseResult<QsBreakStmt>> ParseBreakStmtAsync(QsParserContext context)
        {
            if (!Accept<QsBreakToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            return new QsParseResult<QsBreakStmt>(QsBreakStmt.Instance, context);
        }

        internal async ValueTask<QsParseResult<QsReturnStmt>> ParseReturnStmtAsync(QsParserContext context)
        {
            if (!Accept<QsReturnToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsReturnStmt>.Invalid;
            
            Parse(await parser.ParseExpAsync(context), ref context, out var returnValue);

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsReturnStmt>.Invalid;

            return new QsParseResult<QsReturnStmt>(new QsReturnStmt(returnValue), context);
        }

        internal async ValueTask<QsParseResult<QsBlockStmt>> ParseBlockStmtAsync(QsParserContext context)
        {
            if (!Accept<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBlockStmt>.Invalid;

            var stmts = ImmutableArray.CreateBuilder<QsStmt>();
            while (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (Parse(await ParseStmtAsync(context), ref context, out var stmt))
                {
                    stmts.Add(stmt!);
                    continue;
                }

                return QsParseResult<QsBlockStmt>.Invalid;
            }

            return new QsParseResult<QsBlockStmt>(new QsBlockStmt(stmts.ToImmutable()), context);
        }

        internal async ValueTask<QsParseResult<QsBlankStmt>> ParseBlankStmtAsync(QsParserContext context)
        {
            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBlankStmt>.Invalid;

            return new QsParseResult<QsBlankStmt>(QsBlankStmt.Instance, context);
        }

        // TODO: Assign, Call만 가능하게 해야 한다
        internal async ValueTask<QsParseResult<QsExpStmt>> ParseExpStmtAsync(QsParserContext context)
        {
            if (!Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                return QsParseResult<QsExpStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsExpStmt>.Invalid;

            return new QsParseResult<QsExpStmt>(new QsExpStmt(exp!), context);
        }

        async ValueTask<QsParseResult<QsTaskStmt>> ParseTaskStmtAsync(QsParserContext context)
        {
            if (!Accept<QsTaskToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsTaskStmt>.Invalid;
            
            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<QsTaskStmt>.Invalid; 

            return new QsParseResult<QsTaskStmt>(new QsTaskStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<QsAwaitStmt>> ParseAwaitStmtAsync(QsParserContext context)
        {
            if (!Accept<QsAwaitToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsAwaitStmt>.Invalid;

            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<QsAwaitStmt>.Invalid;

            return new QsParseResult<QsAwaitStmt>(new QsAwaitStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<QsAsyncStmt>> ParseAsyncStmtAsync(QsParserContext context)
        {
            if (!Accept<QsAsyncToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsAsyncStmt>.Invalid;

            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<QsAsyncStmt>.Invalid;

            return new QsParseResult<QsAsyncStmt>(new QsAsyncStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<QsYieldStmt>> ParseYieldStmtAsync(QsParserContext context)
        {
            if (!Accept<QsYieldToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsYieldStmt>.Invalid;

            if (!Parse(await parser.ParseExpAsync(context), ref context, out var yieldValue))
                return QsParseResult<QsYieldStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsYieldStmt>.Invalid;

            return new QsParseResult<QsYieldStmt>(new QsYieldStmt(yieldValue!), context);
        }

        async ValueTask<QsParseResult<QsStringExp>> ParseSingleCommandAsync(QsParserContext context, bool bStopRBrace)
        {
            var stringElems = ImmutableArray.CreateBuilder<QsStringExpElement>();

            // 새 줄이거나 끝에 다다르면 종료
            while (!context.LexerContext.Pos.IsReachEnd())
            {
                if (bStopRBrace && Peek<QsRBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext)))
                    break;

                if (Accept<QsNewLineToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                    break;

                // ${ 이 나오면 
                if (Accept<QsDollarLBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                {
                    // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                        return QsParseResult<QsStringExp>.Invalid;

                    if (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<QsStringExp>.Invalid;

                    stringElems.Add(new QsExpStringExpElement(exp!));
                    continue;
                }

                // aa$b => $b 이야기
                if (Accept<QsIdentifierToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context, out var idToken))
                {
                    stringElems.Add(new QsExpStringExpElement(new QsIdentifierExp(idToken!.Value)));
                    continue;
                }

                if (Accept<QsTextToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context, out var textToken))
                {
                    stringElems.Add(new QsTextStringExpElement(textToken!.Text));
                    continue;
                }

                return QsParseResult<QsStringExp>.Invalid;
            }
            
            return new QsParseResult<QsStringExp>(new QsStringExp(stringElems.ToImmutable()), context);
        }

        internal async ValueTask<QsParseResult<QsForeachStmt>> ParseForeachStmtAsync(QsParserContext context)
        {
            // foreach
            if (!Accept<QsForeachToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsForeachStmt>.Invalid;
            
            // (
            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsForeachStmt>.Invalid;

            // var 
            if (!Parse(await parser.ParseTypeExpAsync(context), ref context, out var typeExp))
                return QsParseResult<QsForeachStmt>.Invalid;

            // x
            if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var varNameToken))
                return QsParseResult<QsForeachStmt>.Invalid;

            // in
            if (!Accept<QsInToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsForeachStmt>.Invalid;

            // obj
            if (!Parse(await parser.ParseExpAsync(context), ref context, out var obj))
                return QsParseResult<QsForeachStmt>.Invalid;

            // )
            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsForeachStmt>.Invalid;

            // stmt
            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var body))
                return QsParseResult<QsForeachStmt>.Invalid;

            return new QsParseResult<QsForeachStmt>(new QsForeachStmt(typeExp!, varNameToken!.Value, obj!, body!), context);
        }

        // 
        internal async ValueTask<QsParseResult<QsCommandStmt>> ParseCommandStmtAsync(QsParserContext context)
        {
            // exec, @로 시작한다
            if (!Accept<QsExecToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsCommandStmt>.Invalid;

            // TODO: optional ()

            // {로 시작한다면 MultiCommand, } 가 나오면 끝난다
            if (Accept<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                // 새줄이거나 끝에 다다르거나 }가 나오면 종료, 
                var commands = ImmutableArray.CreateBuilder<QsStringExp>();
                while (true)
                {
                    if (Accept<QsRBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                        break;

                    if (Parse(await ParseSingleCommandAsync(context, true), ref context, out var singleCommand))                    
                    {
                        // singleCommand Skip 조건
                        if (singleCommand!.Elements.Length == 0)
                            continue;

                        if (singleCommand.Elements.Length == 1 &&
                            singleCommand.Elements[0] is QsTextStringExpElement textElem &&
                            string.IsNullOrWhiteSpace(textElem.Text))
                            continue;

                        commands.Add(singleCommand);
                        continue;
                    }

                    return QsParseResult<QsCommandStmt>.Invalid;
                }

                return new QsParseResult<QsCommandStmt>(new QsCommandStmt(commands.ToImmutable()), context);
            }
            else // 싱글 커맨드, 엔터가 나오면 끝난다
            {
                if (Parse(await ParseSingleCommandAsync(context, false), ref context, out var singleCommand) && 0 < singleCommand!.Elements.Length)
                    return new QsParseResult<QsCommandStmt>(new QsCommandStmt(singleCommand), context);

                return QsParseResult<QsCommandStmt>.Invalid;
            }
        }

        public async ValueTask<QsParseResult<QsStmt>> ParseStmtAsync(QsParserContext context)
        {
            if (Parse(await ParseBlankStmtAsync(context), ref context, out var blankStmt))
                return new QsParseResult<QsStmt>(blankStmt!, context);

            if (Parse(await ParseBlockStmtAsync(context), ref context, out var blockStmt))
                return new QsParseResult<QsStmt>(blockStmt!, context);

            if (Parse(await ParseContinueStmtAsync(context), ref context, out var continueStmt))
                return new QsParseResult<QsStmt>(continueStmt!, context);

            if (Parse(await ParseBreakStmtAsync(context), ref context, out var breakStmt))
                return new QsParseResult<QsStmt>(breakStmt!, context);

            if (Parse(await ParseReturnStmtAsync(context), ref context, out var returnStmt))
                return new QsParseResult<QsStmt>(returnStmt!, context);

            if (Parse(await ParseVarDeclStmtAsync(context), ref context, out var varDeclStmt))
                return new QsParseResult<QsStmt>(varDeclStmt!, context);

            if (Parse(await ParseIfStmtAsync(context), ref context, out var ifStmt))
                return new QsParseResult<QsStmt>(ifStmt!, context);

            if (Parse(await ParseForStmtAsync(context), ref context, out var forStmt))
                return new QsParseResult<QsStmt>(forStmt!, context);

            if (Parse(await ParseExpStmtAsync(context), ref context, out var expStmt))
                return new QsParseResult<QsStmt>(expStmt!, context);

            if (Parse(await ParseTaskStmtAsync(context), ref context, out var taskStmt))
                return new QsParseResult<QsStmt>(taskStmt!, context);

            if (Parse(await ParseAwaitStmtAsync(context), ref context, out var awaitStmt))
                return new QsParseResult<QsStmt>(awaitStmt!, context);

            if (Parse(await ParseAsyncStmtAsync(context), ref context, out var asyncStmt))
                return new QsParseResult<QsStmt>(asyncStmt!, context);

            if (Parse(await ParseForeachStmtAsync(context), ref context, out var foreachStmt))
                return new QsParseResult<QsStmt>(foreachStmt!, context);

            if (Parse(await ParseYieldStmtAsync(context), ref context, out var yieldStmt))
                return new QsParseResult<QsStmt>(yieldStmt!, context);

            if (Parse(await ParseCommandStmtAsync(context), ref context, out var cmdStmt))
                return new QsParseResult<QsStmt>(cmdStmt!, context);

            throw new NotImplementedException();
        }

    }
}