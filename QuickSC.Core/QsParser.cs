using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    using QsExpParseResult = QsParseResult<QsExp>;
    using QsStringExpParseResult = QsParseResult<QsStringExp>;

    public struct QsParserContext
    {
        public QsLexerContext LexerContext { get; }
        ImmutableHashSet<string> types;

        public static QsParserContext Make(QsLexerContext lexerContext)
        {
            return new QsParserContext(lexerContext, ImmutableHashSet<string>.Empty);
        }

        private QsParserContext(QsLexerContext lexerContext, ImmutableHashSet<string> types)
        {
            LexerContext = lexerContext;
            this.types = types;
        }

        public QsParserContext Update(QsLexerContext newContext)
        {
            return new QsParserContext(newContext, types);
        }

        public QsParserContext AddType(string typeName)
        {
            return new QsParserContext(LexerContext, types.Add(typeName));
        }

        public bool HasType(string typeName)
        {
            return true;
        }
    }

    public struct QsParseResult<TSyntaxElem>
    {
        public static QsParseResult<TSyntaxElem> Invalid;
        static QsParseResult()
        {
            Invalid = new QsParseResult<TSyntaxElem>();
        }

        public bool HasValue { get; }
        public TSyntaxElem Elem { get; }
        public QsParserContext Context { get; }
        public QsParseResult(TSyntaxElem elem, QsParserContext context)
        {
            HasValue = true;
            Elem = elem;
            Context = context;
        }
    }

    public class QsParser
    {
        QsLexer lexer;

        public QsParser(QsLexer lexer)
        {
            this.lexer = lexer;
        }

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

        internal async ValueTask<QsExpParseResult> ParseExpAsync(QsParserContext context)
        {
            var boolExpResult = await ParseBoolLiteralExpAsync(context);
            if (boolExpResult.HasValue)
                return new QsExpParseResult(boolExpResult.Elem, boolExpResult.Context);

            var intExpResult = await ParseIntLiteralExpAsync(context);
            if (intExpResult.HasValue)
                return new QsExpParseResult(intExpResult.Elem, intExpResult.Context);

            var stringExpResult = await ParseStringExpAsync(context);
            if (stringExpResult.HasValue)
                return new QsExpParseResult(stringExpResult.Elem, stringExpResult.Context);

            var idExpResult = await ParseIdentifierExpAsync(context);
            if (idExpResult.HasValue)
                return idExpResult;

            return QsExpParseResult.Invalid;
        }

        async ValueTask<QsExpParseResult> ParseBoolLiteralExpAsync(QsParserContext context)
        {
            var boolResult = AcceptAndReturn<QsBoolToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context);
            if (boolResult != null)
                return new QsExpParseResult(new QsBoolLiteralExp(boolResult.Value), context);
            
            return QsExpParseResult.Invalid;
        }

        async ValueTask<QsExpParseResult> ParseIntLiteralExpAsync(QsParserContext context)
        {
            var intResult = AcceptAndReturn<QsIntToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context);
            if (intResult != null)
                return new QsExpParseResult(new QsIntLiteralExp(intResult.Value), context);

            return QsExpParseResult.Invalid;
        }

        // 스트링 파싱
        async ValueTask<QsStringExpParseResult> ParseStringExpAsync(QsParserContext context)
        {
            if (!Accept<QsDoubleQuoteToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsStringExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<QsStringExpElement>();
            while(!Accept<QsDoubleQuoteToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context))
            {   
                var textToken = AcceptAndReturn<QsTextToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if (textToken != null)
                {
                    elems.Add(new QsTextStringExpElement(textToken.Text));
                    continue;
                }

                var idToken = AcceptAndReturn<QsIdentifierToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if(idToken != null)
                {
                    elems.Add(new QsExpStringExpElement(new QsIdentifierExp(idToken.Value)));
                    continue;
                }

                // ${
                if (Accept<QsDollarLBraceToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context))
                {
                    var expResult = await ParseExpAsync(context); // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!expResult.HasValue)
                        return QsStringExpParseResult.Invalid;

                    context = expResult.Context;

                    if (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                        return QsStringExpParseResult.Invalid;

                    elems.Add(new QsExpStringExpElement(expResult.Elem));
                    continue;
                }

                // 나머지는 에러
                return QsStringExpParseResult.Invalid;
            }

            return new QsStringExpParseResult(new QsStringExp(elems.ToImmutable()), context);
        }

        async ValueTask<QsExpParseResult> ParseIdentifierExpAsync(QsParserContext context)
        {
            var idToken = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context);
            if (idToken != null)
                return new QsExpParseResult(new QsIdentifierExp(idToken.Value), context);

            return QsExpParseResult.Invalid;
        }

        internal async ValueTask<QsParseResult<QsIfStmt>> ParseIfStmtAsync(QsParserContext context)
        {
            // if (exp) stmt => If(exp, stmt, null)
            // if (exp) stmt0 else stmt1 => If(exp, stmt0, stmt1)
            // if (exp0) if (exp1) stmt1 else stmt2 => If(exp0, If(exp1, stmt1, stmt2))

            if (!Accept<QsIfToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            var expResult = await ParseExpAsync(context);
            if (!expResult.HasValue)
                return Invalid();

            context = expResult.Context;

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            var bodyResult = await ParseStmtAsync(context); // right assoc, conflict는 별다른 처리를 하지 않고 지나가면 될 것 같다
            if (!bodyResult.HasValue)
                return Invalid();

            context = bodyResult.Context;

            QsStmt? elseBodyStmt = null;
            if (Accept<QsElseToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
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

        // int x = 0;
        internal async ValueTask<QsParseResult<QsVarDeclStmt>> ParseVarDeclStmtAsync(QsParserContext context)
        {
            var typeIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context);

            if (typeIdResult == null)
                return Invalid();

            // var 이면 무사 통과
            if (typeIdResult.Value != "var" && !context.HasType(typeIdResult.Value))
                return Invalid();

            var elems = ImmutableArray.CreateBuilder<QsVarDeclStmtElement>();
            do
            {
                var varIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context);
                if (varIdResult == null)
                    return Invalid();

                QsExp? initExp = null;
                if (Accept<QsEqualToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                {
                    var expResult = await ParseExpAsync(context); // TODO: ;나 ,가 나올때까지라는걸 명시해주면 좋겠다
                    if (!expResult.HasValue)
                        return Invalid();

                    initExp = expResult.Elem;
                    context = expResult.Context;
                }

                elems.Add(new QsVarDeclStmtElement(varIdResult.Value, initExp));

            } while (Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context)); // ,가 나오면 계속한다

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context)) // ;으로 마무리
                return Invalid();

            return new QsParseResult<QsVarDeclStmt>(new QsVarDeclStmt(typeIdResult.Value, elems.ToImmutable()), context);

            static QsParseResult<QsVarDeclStmt> Invalid() => QsParseResult<QsVarDeclStmt>.Invalid;
        }

        async ValueTask<QsParseResult<QsForStmtInitializer>> ParseForStmtInitializerAsync(QsParserContext context)
        {
            return QsParseResult<QsForStmtInitializer>.Invalid;
        }

        async ValueTask<QsParseResult<QsForStmt>> ParseForStmtAsync(QsParserContext context)
        {
            // TODO: Invalid와 Fatal을 구분해야 할 것 같다.. 어찌할지는 깊게 생각을 해보자
            if (!Accept<QsForToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();
            
            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            QsForStmtInitializer? initializer = null;
            // TODO: 이 Initializer의 끝은 ';' 이다
            var initializerResult = await ParseForStmtInitializerAsync(context);
            if (initializerResult.HasValue)
            {
                initializer = initializerResult.Elem;
                context = initializerResult.Context;
            }

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            // TODO: 이 CondExp의 끝은 ';' 이다
            QsExp? condExp = null;
            var condExpResult = await ParseExpAsync(context);
            if (condExpResult.HasValue)
            {
                condExp = condExpResult.Elem;
                context = condExpResult.Context;
            }

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            QsExp? contExp = null;
            // TODO: 이 CondExp의 끝은 ')' 이다            
            var contExpResult = await ParseExpAsync(context);
            if (condExpResult.HasValue)
            {
                contExp = contExpResult.Elem;
                context = contExpResult.Context;
            }

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return Invalid();

            var bodyStmtResult = await ParseStmtAsync(context);
            if (!bodyStmtResult.HasValue)
                return Invalid();

            context = bodyStmtResult.Context;

            return new QsParseResult<QsForStmt>(new QsForStmt(initializer, condExp, contExp, bodyStmtResult.Elem), context);

            static QsParseResult<QsForStmt> Invalid() => QsParseResult<QsForStmt>.Invalid;
        }

        async ValueTask<QsParseResult<QsBlankStmt>> ParseBlankStmtAsync(QsParserContext context)
        {
            if (Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return new QsParseResult<QsBlankStmt>(QsBlankStmt.Instance, context);

            return QsParseResult<QsBlankStmt>.Invalid;
        }

        async ValueTask<QsParseResult<QsContinueStmt>> ParseContinueStmtAsync(QsParserContext context)
        {
            if (!Accept<QsContinueStmt>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsParseResult<QsContinueStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsParseResult<QsContinueStmt>.Invalid;

            return new QsParseResult<QsContinueStmt>(QsContinueStmt.Instance, context);
        }

        async ValueTask<QsParseResult<QsBreakStmt>> ParseBreakStmtAsync(QsParserContext context)
        {
            if (!Accept<QsContinueStmt>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            if (!Accept<QsSemiColonToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsParseResult<QsBreakStmt>.Invalid;

            return new QsParseResult<QsBreakStmt>(QsBreakStmt.Instance, context);
        }

        async ValueTask<QsParseResult<QsBlockStmt>> ParseBlockStmtAsync(QsParserContext context)
        {
            if (!Accept<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsParseResult<QsBlockStmt>.Invalid;

            var stmts = ImmutableArray.CreateBuilder<QsStmt>();
            while (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
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

        async ValueTask<QsParseResult<QsCommandStmt>> ParseCommandStmtAsync(QsParserContext context)
        {
            //  첫 <NEWLINE>, <WS>는 넘기는데, 
            // TODO: <NEWLINE> 또는 '파일 시작'이 무조건 하나는 있어야 한다
            while (!context.LexerContext.Pos.IsReachEnd())
            {
                if (Accept<QsWhitespaceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context)) continue;

                if (Accept<QsEndOfCommandToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context)) continue;

                break;
            }

            var stringElems = ImmutableArray.CreateBuilder<QsStringExpElement>();
            var exps = ImmutableArray.CreateBuilder<QsExp>();
            while (!Accept<QsEndOfCommandToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
            {
                if (Accept<QsWhitespaceToken>(await lexer.LexCommandModeAsync(context.LexerContext), ref context))
                {
                    Debug.Assert(0 < stringElems.Count);
                    exps.Add(new QsStringExp(stringElems.ToImmutable()));
                    stringElems.Clear();
                    continue;
                }

                // aa"bbb ccc" => 
                var stringResult = await ParseStringExpAsync(context);
                if (stringResult.HasValue)
                {
                    stringElems.AddRange(stringResult.Elem.Elements); // flatten
                    context = stringResult.Context;
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

                return QsParseResult<QsCommandStmt>.Invalid;
            }

            // 지금까지 모아놓은거 또 flush
            if (0 < stringElems.Count)
            {
                exps.Add(new QsStringExp(stringElems.ToImmutable()));
                stringElems.Clear();
            }

            if (exps.Count == 0) return QsParseResult<QsCommandStmt>.Invalid;

            return new QsParseResult<QsCommandStmt>(new QsCommandStmt(exps[0], exps.Skip(1).ToImmutableArray()), context);
        }

        public async ValueTask<QsParseResult<QsStmt>> ParseStmtAsync(QsParserContext context)
        {
            var blankStmtResult = await ParseBlankStmtAsync(context);
            if (blankStmtResult.HasValue)
                return Result(blankStmtResult);

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

            var cmdResult = await ParseCommandStmtAsync(context);
            if (cmdResult.HasValue) 
                return Result(cmdResult);

            throw new NotImplementedException();

            static QsParseResult<QsStmt> Result<TStmt>(QsParseResult<TStmt> result) where TStmt : QsStmt
            {
                return new QsParseResult<QsStmt>(result.Elem, result.Context);
            }
        }

        async ValueTask<QsParseResult<QsScriptElement>> ParseScriptElementAsync(QsParserContext context)
        {
            var stmtResult = await ParseStmtAsync(context);
            if (stmtResult.HasValue) 
                return new QsParseResult<QsScriptElement>(new QsStmtScriptElement(stmtResult.Elem), stmtResult.Context);

            return new QsParseResult<QsScriptElement>();
        }

        public async ValueTask<QsParseResult<QsScript>> ParseScriptAsync(QsParserContext context)
        {
            var elems = ImmutableArray.CreateBuilder<QsScriptElement>();

            while (!Accept<QsEndOfFileToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
            {
                var elemResult = await ParseScriptElementAsync(context);
                if (!elemResult.HasValue) return QsParseResult<QsScript>.Invalid;

                elems.Add(elemResult.Elem);
                context = elemResult.Context;
            }

            return new QsParseResult<QsScript>(new QsScript(elems.ToImmutable()), context);
        }
    }
}