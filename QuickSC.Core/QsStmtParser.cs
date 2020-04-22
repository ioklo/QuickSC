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
        QsLexer lexer;
        QsExpParser expParser;

        #region Utilities
        bool Accept<TToken>(QsLexResult lexResult, ref QsParserContext context)
        {
            if (lexResult.HasValue && lexResult.Token is TToken)
            {
                context = context.Update(lexResult.Context);
                return true;
            }

            return false;
        }

        TToken? AcceptAndReturn<TToken>(QsLexResult lexResult, ref QsParserContext context) where TToken : QsToken
        {
            if (lexResult.HasValue && lexResult.Token is TToken token)
            {
                context = context.Update(lexResult.Context);
                return token;
            }

            return null;
        }

        bool Peek<TToken>(QsLexResult lexResult) where TToken : QsToken
        {
            return lexResult.HasValue && lexResult.Token is TToken;
        }
        #endregion

        public QsStmtParser(QsLexer lexer)
        {
            this.lexer = lexer;
            this.expParser = new QsExpParser(lexer);
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

            var expResult = await expParser.ParseExpAsync(context);
            if (!expResult.HasValue)
                return Invalid();

            context = expResult.Context;

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            var bodyResult = await ParseStmtAsync(context); // right assoc, conflict는 별다른 처리를 하지 않고 지나가면 될 것 같다
            if (!bodyResult.HasValue)
                return Invalid();

            context = bodyResult.Context;

            QsStmt? elseBodyStmt = null;
            if (Accept<QsElseToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                var elseBodyResult = await ParseStmtAsync(context);
                if (!elseBodyResult.HasValue)
                    return Invalid();

                elseBodyStmt = elseBodyResult.Elem;
                context = elseBodyResult.Context;
            }

            return new QsParseResult<QsIfStmt>(new QsIfStmt(expResult.Elem, bodyResult.Elem, elseBodyStmt), context);

            static QsParseResult<QsIfStmt> Invalid() => QsParseResult<QsIfStmt>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsVarDecl>> ParseVarDeclAsync(QsParserContext context)
        {
            var typeIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);

            if (typeIdResult == null)
                return Invalid();

            // var 이면 무사 통과
            if (typeIdResult.Value != "var" && !context.HasType(typeIdResult.Value))
                return Invalid();

            var elems = ImmutableArray.CreateBuilder<QsVarDeclElement>();
            do
            {
                var varIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                if (varIdResult == null)
                    return Invalid();

                QsExp? initExp = null;
                if (Accept<QsEqualToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    var expResult = await expParser.ParseExpAsync(context); // TODO: ;나 ,가 나올때까지라는걸 명시해주면 좋겠다
                    if (!expResult.HasValue)
                        return Invalid();

                    initExp = expResult.Elem;
                    context = expResult.Context;
                }

                elems.Add(new QsVarDeclElement(varIdResult.Value, initExp));

            } while (Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)); // ,가 나오면 계속한다

            return new QsParseResult<QsVarDecl>(new QsVarDecl(typeIdResult.Value, elems.ToImmutable()), context);

            static QsParseResult<QsVarDecl> Invalid() => QsParseResult<QsVarDecl>.Invalid;
        }

        // int x = 0;
        internal async ValueTask<QsParseResult<QsVarDeclStmt>> ParseVarDeclStmtAsync(QsParserContext context)
        {
            var varDeclResult = await ParseVarDeclAsync(context);
            if (!varDeclResult.HasValue)
                return Invalid();

            context = varDeclResult.Context;

            if (!context.LexerContext.Pos.IsReachEnd() &&
                !Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)) // ;으로 마무리
                return Invalid();

            return new QsParseResult<QsVarDeclStmt>(new QsVarDeclStmt(varDeclResult.Elem), context);


            static QsParseResult<QsVarDeclStmt> Invalid() => QsParseResult<QsVarDeclStmt>.Invalid;
        }

        async ValueTask<QsParseResult<QsForStmtInitializer>> ParseForStmtInitializerAsync(QsParserContext context)
        {
            var varDeclResult = await ParseVarDeclAsync(context);
            if (varDeclResult.HasValue)
                return new QsParseResult<QsForStmtInitializer>(new QsVarDeclForStmtInitializer(varDeclResult.Elem), varDeclResult.Context);

            var expResult = await expParser.ParseExpAsync(context);
            if (expResult.HasValue)
                return new QsParseResult<QsForStmtInitializer>(new QsExpForStmtInitializer(expResult.Elem), expResult.Context);

            return QsParseResult<QsForStmtInitializer>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsForStmt>> ParseForStmtAsync(QsParserContext context)
        {
            // TODO: Invalid와 Fatal을 구분해야 할 것 같다.. 어찌할지는 깊게 생각을 해보자
            if (!Accept<QsForToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            QsForStmtInitializer? initializer = null;
            // TODO: 이 Initializer의 끝은 ';' 이다
            var initializerResult = await ParseForStmtInitializerAsync(context);
            if (initializerResult.HasValue)
            {
                initializer = initializerResult.Elem;
                context = initializerResult.Context;
            }

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // TODO: 이 CondExp의 끝은 ';' 이다
            QsExp? condExp = null;
            var condExpResult = await expParser.ParseExpAsync(context);
            if (condExpResult.HasValue)
            {
                condExp = condExpResult.Elem;
                context = condExpResult.Context;
            }

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            QsExp? contExp = null;
            // TODO: 이 CondExp의 끝은 ')' 이다            
            var contExpResult = await expParser.ParseExpAsync(context);
            if (condExpResult.HasValue)
            {
                contExp = contExpResult.Elem;
                context = contExpResult.Context;
            }

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            var bodyStmtResult = await ParseStmtAsync(context);
            if (!bodyStmtResult.HasValue)
                return Invalid();

            context = bodyStmtResult.Context;

            return new QsParseResult<QsForStmt>(new QsForStmt(initializer, condExp, contExp, bodyStmtResult.Elem), context);

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
            if (!Accept<QsContinueToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            return new QsParseResult<QsBreakStmt>(QsBreakStmt.Instance, context);
        }

        internal async ValueTask<QsParseResult<QsBlockStmt>> ParseBlockStmtAsync(QsParserContext context)
        {
            if (!Accept<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsBlockStmt>.Invalid;

            var stmts = ImmutableArray.CreateBuilder<QsStmt>();
            while (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {   
                var stmtResult = await ParseStmtAsync(context);
                if (stmtResult.HasValue)
                {
                    context = stmtResult.Context;
                    stmts.Add(stmtResult.Elem);

                    continue;
                }

                return QsParseResult<QsBlockStmt>.Invalid;
            }

            return new QsParseResult<QsBlockStmt>(new QsBlockStmt(stmts.ToImmutable()), context);
        }

        // TODO: Assign, Call만 가능하게 해야 한다
        internal async ValueTask<QsParseResult<QsExpStmt>> ParseExpStmtAsync(QsParserContext context)
        {
            var expResult = await expParser.ParseExpAsync(context);
            if (!expResult.HasValue) return QsParseResult<QsExpStmt>.Invalid;

            context = expResult.Context;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsExpStmt>.Invalid;

            return new QsParseResult<QsExpStmt>(new QsExpStmt(expResult.Elem), context);
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
                    var expResult = await expParser.ParseExpAsync(context); // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!expResult.HasValue)
                        return QsParseResult<QsStringExp>.Invalid;

                    context = expResult.Context;

                    if (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<QsStringExp>.Invalid;

                    stringElems.Add(new QsExpStringExpElement(expResult.Elem));
                    continue;
                }

                // aa$b => $b 이야기
                var idToken = AcceptAndReturn<QsIdentifierToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context);
                if (idToken != null)
                {
                    stringElems.Add(new QsExpStringExpElement(new QsIdentifierExp(idToken.Value)));
                    continue;
                }

                var textToken = AcceptAndReturn<QsTextToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context);
                if (textToken != null)
                {
                    stringElems.Add(new QsTextStringExpElement(textToken.Text));
                    continue;
                }

                return QsParseResult<QsStringExp>.Invalid;
            }

            // 지금까지 모아놓은거 또 flush
            if (stringElems.Count == 0)
                return QsParseResult<QsStringExp>.Invalid;

            return new QsParseResult<QsStringExp>(new QsStringExp(stringElems.ToImmutable()), context);
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

                    var singleCommandResult = await ParseSingleCommandAsync(context, true);
                    if (singleCommandResult.HasValue)
                    {
                        // singleCommand Skip 조건
                        if (singleCommandResult.Elem.Elements.Length != 1 ||
                            !(singleCommandResult.Elem.Elements[0] is QsTextStringExpElement textElem) ||
                            !string.IsNullOrWhiteSpace(textElem.Text))
                            commands.Add(singleCommandResult.Elem);

                        context = singleCommandResult.Context;
                        continue;
                    }

                    return QsParseResult<QsCommandStmt>.Invalid;
                }

                return new QsParseResult<QsCommandStmt>(new QsCommandStmt(commands.ToImmutable()), context);
            }
            else // 싱글 커맨드, 엔터가 나오면 끝난다
            {
                var singleCommandResult = await ParseSingleCommandAsync(context, false);
                if (singleCommandResult.HasValue)
                    return new QsParseResult<QsCommandStmt>(new QsCommandStmt(singleCommandResult.Elem), singleCommandResult.Context);
            }

            return QsParseResult<QsCommandStmt>.Invalid;
        }

        public async ValueTask<QsParseResult<QsStmt>> ParseStmtAsync(QsParserContext context)
        {
            var blockStmtResult = await ParseBlockStmtAsync(context);
            if (blockStmtResult.HasValue)
                return Result(blockStmtResult);

            var continueStmtResult = await ParseContinueStmtAsync(context);
            if (continueStmtResult.HasValue)
                return Result(continueStmtResult);

            var breakStmtResult = await ParseBreakStmtAsync(context);
            if (breakStmtResult.HasValue)
                return Result(breakStmtResult);

            var varDeclResult = await ParseVarDeclStmtAsync(context);
            if (varDeclResult.HasValue)
                return Result(varDeclResult);

            var ifStmtResult = await ParseIfStmtAsync(context);
            if (ifStmtResult.HasValue)
                return Result(ifStmtResult);

            var forStmtResult = await ParseForStmtAsync(context);
            if (forStmtResult.HasValue)
                return Result(forStmtResult);

            var expStmtResult = await ParseExpStmtAsync(context);
            if (expStmtResult.HasValue)
                return Result(expStmtResult);

            var cmdResult = await ParseCommandStmtAsync(context);
            if (cmdResult.HasValue)
                return Result(cmdResult);

            throw new NotImplementedException();

            static QsParseResult<QsStmt> Result<TStmt>(QsParseResult<TStmt> result) where TStmt : QsStmt
            {
                return new QsParseResult<QsStmt>(result.Elem, result.Context);
            }
        }

    }
}