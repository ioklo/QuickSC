using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gum.LexicalAnalysis;
using Gum.Syntax;

namespace Gum
{
    public class StmtParser
    {
        Parser parser;
        Lexer lexer;

        #region Utilities
        bool Accept<TToken>(LexResult lexResult, ref ParserContext context) where TToken : Token
        {
            if (lexResult.HasValue && lexResult.Token is TToken)
            {
                context = context.Update(lexResult.Context);
                return true;
            }

            return false;
        }

        bool Accept<TToken>(LexResult lexResult, ref ParserContext context, out TToken? token) where TToken : Token
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

        bool Peek<TToken>(LexResult lexResult) where TToken : Token
        {
            return lexResult.HasValue && lexResult.Token is TToken;
        }

        bool Parse<TSyntaxElem>(QsParseResult<TSyntaxElem> parseResult, ref ParserContext context, out TSyntaxElem? elem) where TSyntaxElem : class
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

        public StmtParser(Parser parser, Lexer lexer)
        {
            this.parser = parser;
            this.lexer = lexer;
        }
        
        internal async ValueTask<QsParseResult<IfStmt>> ParseIfStmtAsync(ParserContext context)
        {
            // if (exp) stmt => If(exp, stmt, null)
            // if (exp) stmt0 else stmt1 => If(exp, stmt0, stmt1)
            // if (exp0) if (exp1) stmt1 else stmt2 => If(exp0, If(exp1, stmt1, stmt2))
            // if (exp is typeExp) 

            if (!Accept<IfToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Parse(await parser.ParseExpAsync(context), ref context, out var cond))            
                return Invalid();

            // 
            TypeExp? condTestType = null;
            if (Accept<IsToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                if (!Parse(await parser.ParseTypeExpAsync(context), ref context, out condTestType))
                    return Invalid();

            if (!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // right assoc, conflict는 별다른 처리를 하지 않고 지나가면 될 것 같다
            if (!Parse(await ParseStmtAsync(context), ref context, out var body))
                return Invalid();

            Stmt? elseBody = null;
            if (Accept<ElseToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (!Parse(await ParseStmtAsync(context), ref context, out elseBody))
                    return Invalid();
            }

            return new QsParseResult<IfStmt>(new IfStmt(cond!, condTestType, body!, elseBody), context);

            static QsParseResult<IfStmt> Invalid() => QsParseResult<IfStmt>.Invalid;
        }

        internal async ValueTask<QsParseResult<VarDecl>> ParseVarDeclAsync(ParserContext context)
        {
            if (!Parse(await parser.ParseTypeExpAsync(context), ref context, out var varType))
                return Invalid();

            var elems = ImmutableArray.CreateBuilder<VarDeclElement>();
            do
            {
                if (!Accept<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var varIdResult))
                    return Invalid();

                Exp? initExp = null;
                if (Accept<EqualToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    // TODO: ;나 ,가 나올때까지라는걸 명시해주면 좋겠다
                    if (!Parse(await parser.ParseExpAsync(context), ref context, out initExp))
                        return Invalid();
                }

                elems.Add(new VarDeclElement(varIdResult!.Value, initExp));

            } while (Accept<CommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)); // ,가 나오면 계속한다

            return new QsParseResult<VarDecl>(new VarDecl(varType!, elems.ToImmutable()), context);

            static QsParseResult<VarDecl> Invalid() => QsParseResult<VarDecl>.Invalid;
        }

        // int x = 0;
        internal async ValueTask<QsParseResult<VarDeclStmt>> ParseVarDeclStmtAsync(ParserContext context)
        {
            if (!Parse(await ParseVarDeclAsync(context), ref context, out var varDecl))
                return QsParseResult<VarDeclStmt>.Invalid;

            if (!context.LexerContext.Pos.IsReachEnd() &&
                !Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context)) // ;으로 마무리
                return QsParseResult<VarDeclStmt>.Invalid;

            return new QsParseResult<VarDeclStmt>(new VarDeclStmt(varDecl!), context);
        }

        async ValueTask<QsParseResult<ForStmtInitializer>> ParseForStmtInitializerAsync(ParserContext context)
        {
            if (Parse(await ParseVarDeclAsync(context), ref context, out var varDecl))            
                return new QsParseResult<ForStmtInitializer>(new VarDeclForStmtInitializer(varDecl!), context);

            if (Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                return new QsParseResult<ForStmtInitializer>(new ExpForStmtInitializer(exp!), context);

            return QsParseResult<ForStmtInitializer>.Invalid;
        }

        internal async ValueTask<QsParseResult<ForStmt>> ParseForStmtAsync(ParserContext context)
        {
            // TODO: Invalid와 Fatal을 구분해야 할 것 같다.. 어찌할지는 깊게 생각을 해보자
            if (!Accept<ForToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();
            
            // TODO: 이 Initializer의 끝은 ';' 이다
            Parse(await ParseForStmtInitializerAsync(context), ref context, out var initializer);
            
            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // TODO: 이 CondExp의 끝은 ';' 이다            
            Parse(await parser.ParseExpAsync(context), ref context, out var cond);

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();
            
            // TODO: 이 CondExp의 끝은 ')' 이다            
            Parse(await parser.ParseExpAsync(context), ref context, out var cont);
            
            if (!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            if (!Parse(await ParseStmtAsync(context), ref context, out var bodyStmt))            
                return Invalid();

            return new QsParseResult<ForStmt>(new ForStmt(initializer, cond, cont, bodyStmt!), context);

            static QsParseResult<ForStmt> Invalid() => QsParseResult<ForStmt>.Invalid;
        }

        internal async ValueTask<QsParseResult<ContinueStmt>> ParseContinueStmtAsync(ParserContext context)
        {
            if (!Accept<ContinueToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ContinueStmt>.Invalid;

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ContinueStmt>.Invalid;

            return new QsParseResult<ContinueStmt>(ContinueStmt.Instance, context);
        }

        internal async ValueTask<QsParseResult<BreakStmt>> ParseBreakStmtAsync(ParserContext context)
        {
            if (!Accept<BreakToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<BreakStmt>.Invalid;

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<BreakStmt>.Invalid;

            return new QsParseResult<BreakStmt>(BreakStmt.Instance, context);
        }

        internal async ValueTask<QsParseResult<ReturnStmt>> ParseReturnStmtAsync(ParserContext context)
        {
            if (!Accept<ReturnToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ReturnStmt>.Invalid;
            
            Parse(await parser.ParseExpAsync(context), ref context, out var returnValue);

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ReturnStmt>.Invalid;

            return new QsParseResult<ReturnStmt>(new ReturnStmt(returnValue), context);
        }

        internal async ValueTask<QsParseResult<BlockStmt>> ParseBlockStmtAsync(ParserContext context)
        {
            if (!Accept<LBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<BlockStmt>.Invalid;

            var stmts = ImmutableArray.CreateBuilder<Stmt>();
            while (!Accept<RBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (Parse(await ParseStmtAsync(context), ref context, out var stmt))
                {
                    stmts.Add(stmt!);
                    continue;
                }

                return QsParseResult<BlockStmt>.Invalid;
            }

            return new QsParseResult<BlockStmt>(new BlockStmt(stmts.ToImmutable()), context);
        }

        internal async ValueTask<QsParseResult<BlankStmt>> ParseBlankStmtAsync(ParserContext context)
        {
            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<BlankStmt>.Invalid;

            return new QsParseResult<BlankStmt>(BlankStmt.Instance, context);
        }

        // TODO: Assign, Call만 가능하게 해야 한다
        internal async ValueTask<QsParseResult<ExpStmt>> ParseExpStmtAsync(ParserContext context)
        {
            if (!Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                return QsParseResult<ExpStmt>.Invalid;

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ExpStmt>.Invalid;

            return new QsParseResult<ExpStmt>(new ExpStmt(exp!), context);
        }

        async ValueTask<QsParseResult<TaskStmt>> ParseTaskStmtAsync(ParserContext context)
        {
            if (!Accept<TaskToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<TaskStmt>.Invalid;
            
            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<TaskStmt>.Invalid; 

            return new QsParseResult<TaskStmt>(new TaskStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<AwaitStmt>> ParseAwaitStmtAsync(ParserContext context)
        {
            if (!Accept<AwaitToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<AwaitStmt>.Invalid;

            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<AwaitStmt>.Invalid;

            return new QsParseResult<AwaitStmt>(new AwaitStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<AsyncStmt>> ParseAsyncStmtAsync(ParserContext context)
        {
            if (!Accept<AsyncToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<AsyncStmt>.Invalid;

            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var stmt))
                return QsParseResult<AsyncStmt>.Invalid;

            return new QsParseResult<AsyncStmt>(new AsyncStmt(stmt!), context);
        }

        async ValueTask<QsParseResult<YieldStmt>> ParseYieldStmtAsync(ParserContext context)
        {
            if (!Accept<YieldToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<YieldStmt>.Invalid;

            if (!Parse(await parser.ParseExpAsync(context), ref context, out var yieldValue))
                return QsParseResult<YieldStmt>.Invalid;

            if (!Accept<SemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<YieldStmt>.Invalid;

            return new QsParseResult<YieldStmt>(new YieldStmt(yieldValue!), context);
        }

        async ValueTask<QsParseResult<StringExp>> ParseSingleCommandAsync(ParserContext context, bool bStopRBrace)
        {
            var stringElems = ImmutableArray.CreateBuilder<StringExpElement>();

            // 새 줄이거나 끝에 다다르면 종료
            while (!context.LexerContext.Pos.IsReachEnd())
            {
                if (bStopRBrace && Peek<RBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext)))
                    break;

                if (Accept<NewLineToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                    break;

                // ${ 이 나오면 
                if (Accept<DollarLBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                {
                    // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!Parse(await parser.ParseExpAsync(context), ref context, out var exp))
                        return QsParseResult<StringExp>.Invalid;

                    if (!Accept<RBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<StringExp>.Invalid;

                    stringElems.Add(new ExpStringExpElement(exp!));
                    continue;
                }

                // aa$b => $b 이야기
                if (Accept<IdentifierToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context, out var idToken))
                {
                    stringElems.Add(new ExpStringExpElement(new IdentifierExp(idToken!.Value)));
                    continue;
                }

                if (Accept<TextToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context, out var textToken))
                {
                    stringElems.Add(new TextStringExpElement(textToken!.Text));
                    continue;
                }

                return QsParseResult<StringExp>.Invalid;
            }
            
            return new QsParseResult<StringExp>(new StringExp(stringElems.ToImmutable()), context);
        }

        internal async ValueTask<QsParseResult<ForeachStmt>> ParseForeachStmtAsync(ParserContext context)
        {
            // foreach
            if (!Accept<ForeachToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ForeachStmt>.Invalid;
            
            // (
            if (!Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ForeachStmt>.Invalid;

            // var 
            if (!Parse(await parser.ParseTypeExpAsync(context), ref context, out var typeExp))
                return QsParseResult<ForeachStmt>.Invalid;

            // x
            if (!Accept<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var varNameToken))
                return QsParseResult<ForeachStmt>.Invalid;

            // in
            if (!Accept<InToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ForeachStmt>.Invalid;

            // obj
            if (!Parse(await parser.ParseExpAsync(context), ref context, out var obj))
                return QsParseResult<ForeachStmt>.Invalid;

            // )
            if (!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ForeachStmt>.Invalid;

            // stmt
            if (!Parse(await parser.ParseStmtAsync(context), ref context, out var body))
                return QsParseResult<ForeachStmt>.Invalid;

            return new QsParseResult<ForeachStmt>(new ForeachStmt(typeExp!, varNameToken!.Value, obj!, body!), context);
        }

        // 
        internal async ValueTask<QsParseResult<CommandStmt>> ParseCommandStmtAsync(ParserContext context)
        {
            // exec, @로 시작한다
            if (!Accept<ExecToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<CommandStmt>.Invalid;

            // TODO: optional ()

            // {로 시작한다면 MultiCommand, } 가 나오면 끝난다
            if (Accept<LBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                // 새줄이거나 끝에 다다르거나 }가 나오면 종료, 
                var commands = ImmutableArray.CreateBuilder<StringExp>();
                while (true)
                {
                    if (Accept<RBraceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                        break;

                    if (Parse(await ParseSingleCommandAsync(context, true), ref context, out var singleCommand))                    
                    {
                        // singleCommand Skip 조건
                        if (singleCommand!.Elements.Length == 0)
                            continue;

                        if (singleCommand.Elements.Length == 1 &&
                            singleCommand.Elements[0] is TextStringExpElement textElem &&
                            string.IsNullOrWhiteSpace(textElem.Text))
                            continue;

                        commands.Add(singleCommand);
                        continue;
                    }

                    return QsParseResult<CommandStmt>.Invalid;
                }

                return new QsParseResult<CommandStmt>(new CommandStmt(commands.ToImmutable()), context);
            }
            else // 싱글 커맨드, 엔터가 나오면 끝난다
            {
                if (Parse(await ParseSingleCommandAsync(context, false), ref context, out var singleCommand) && 0 < singleCommand!.Elements.Length)
                    return new QsParseResult<CommandStmt>(new CommandStmt(singleCommand), context);

                return QsParseResult<CommandStmt>.Invalid;
            }
        }

        public async ValueTask<QsParseResult<Stmt>> ParseStmtAsync(ParserContext context)
        {
            if (Parse(await ParseBlankStmtAsync(context), ref context, out var blankStmt))
                return new QsParseResult<Stmt>(blankStmt!, context);

            if (Parse(await ParseBlockStmtAsync(context), ref context, out var blockStmt))
                return new QsParseResult<Stmt>(blockStmt!, context);

            if (Parse(await ParseContinueStmtAsync(context), ref context, out var continueStmt))
                return new QsParseResult<Stmt>(continueStmt!, context);

            if (Parse(await ParseBreakStmtAsync(context), ref context, out var breakStmt))
                return new QsParseResult<Stmt>(breakStmt!, context);

            if (Parse(await ParseReturnStmtAsync(context), ref context, out var returnStmt))
                return new QsParseResult<Stmt>(returnStmt!, context);

            if (Parse(await ParseVarDeclStmtAsync(context), ref context, out var varDeclStmt))
                return new QsParseResult<Stmt>(varDeclStmt!, context);

            if (Parse(await ParseIfStmtAsync(context), ref context, out var ifStmt))
                return new QsParseResult<Stmt>(ifStmt!, context);

            if (Parse(await ParseForStmtAsync(context), ref context, out var forStmt))
                return new QsParseResult<Stmt>(forStmt!, context);

            if (Parse(await ParseExpStmtAsync(context), ref context, out var expStmt))
                return new QsParseResult<Stmt>(expStmt!, context);

            if (Parse(await ParseTaskStmtAsync(context), ref context, out var taskStmt))
                return new QsParseResult<Stmt>(taskStmt!, context);

            if (Parse(await ParseAwaitStmtAsync(context), ref context, out var awaitStmt))
                return new QsParseResult<Stmt>(awaitStmt!, context);

            if (Parse(await ParseAsyncStmtAsync(context), ref context, out var asyncStmt))
                return new QsParseResult<Stmt>(asyncStmt!, context);

            if (Parse(await ParseForeachStmtAsync(context), ref context, out var foreachStmt))
                return new QsParseResult<Stmt>(foreachStmt!, context);

            if (Parse(await ParseYieldStmtAsync(context), ref context, out var yieldStmt))
                return new QsParseResult<Stmt>(yieldStmt!, context);

            if (Parse(await ParseCommandStmtAsync(context), ref context, out var cmdStmt))
                return new QsParseResult<Stmt>(cmdStmt!, context);

            throw new NotImplementedException();
        }

    }
}