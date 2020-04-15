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

        ValueTask<QsLexResult> LexAsync(QsParserContext context)
        {
            return lexer.LexAsync(context.LexerContext);
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
            var stringExpResult = await ParseStringExpAsync(context);
            if (stringExpResult.HasValue)
                return new QsExpParseResult(stringExpResult.Elem, stringExpResult.Context);

            var idExpResult = await ParseIdentifierExpAsync(context);
            if (idExpResult.HasValue)
                return idExpResult;

            return QsExpParseResult.Invalid;
        }
        
        // 스트링 파싱
        async ValueTask<QsStringExpParseResult> ParseStringExpAsync(QsParserContext context)
        {
            if (!Accept<QsBeginStringToken>(await LexAsync(context), ref context))
                return QsStringExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<QsStringExpElement>();
            while(!Accept<QsEndStringToken>(await LexAsync(context), ref context))
            {   
                var textToken = AcceptAndReturn<QsTextToken>(await LexAsync(context), ref context);
                if (textToken != null)
                {
                    elems.Add(new QsTextStringExpElement(textToken.Text));
                    continue;
                }

                var idToken = AcceptAndReturn<QsIdentifierToken>(await LexAsync(context), ref context);
                if(idToken != null)
                {
                    elems.Add(new QsExpStringExpElement(new QsIdentifierExp(idToken.Value)));
                    continue;
                }

                if (Accept<QsBeginInnerExpToken>(await LexAsync(context), ref context))
                {
                    var expResult = await ParseExpAsync(context); // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!expResult.HasValue)
                        return QsStringExpParseResult.Invalid;

                    context = expResult.Context;

                    if (!Accept<QsEndInnerExpToken>(await LexAsync(context), ref context))
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
            var idToken = AcceptAndReturn<QsIdentifierToken>(await LexAsync(context), ref context);
            if (idToken != null)
                return new QsExpParseResult(new QsIdentifierExp(idToken.Value), context);

            return QsExpParseResult.Invalid;
        }

        // int x = 0;
        internal async ValueTask<QsParseResult<QsVarDeclStmt>> ParseVarDeclStmtAsync(QsParserContext context)
        {
            var typeIdResult = AcceptAndReturn<QsIdentifierToken>(await LexAsync(context), ref context);

            if (typeIdResult == null)
                return Invalid();

            if (!context.HasType(typeIdResult.Value))
                return Invalid();

            var elems = ImmutableArray.CreateBuilder<QsVarDeclStmtElement>();
            do
            {
                var varIdResult = AcceptAndReturn<QsIdentifierToken>(await LexAsync(context), ref context);
                if (varIdResult == null)
                    return Invalid();

                QsExp? initExp = null;
                if (Accept<QsEqualToken>(await LexAsync(context), ref context))
                {
                    var expResult = await ParseExpAsync(context); // TODO: ;나 ,가 나올때까지라는걸 명시해주면 좋겠다
                    if (!expResult.HasValue)
                        return Invalid();

                    initExp = expResult.Elem;
                    context = expResult.Context;
                }

                elems.Add(new QsVarDeclStmtElement(varIdResult.Value, initExp));

            } while (Accept<QsCommaToken>(await LexAsync(context), ref context)); // ,가 나오면 계속한다

            if (!Accept<QsSemiColonToken>(await LexAsync(context), ref context)) // ;으로 마무리
                return Invalid();

            return new QsParseResult<QsVarDeclStmt>(new QsVarDeclStmt(typeIdResult.Value, elems.ToImmutable()), context);

            static QsParseResult<QsVarDeclStmt> Invalid() => QsParseResult<QsVarDeclStmt>.Invalid;
        }
        

        // 커맨드란
        async ValueTask<QsParseResult<QsCommandStmt>> ParseCommandStmtAsync(QsParserContext context)
        {
            // TODO: 이걸 여기서 해야하나
            context = context.Update(context.LexerContext.UpdateMode(QsLexingMode.Command));

            // 첫 WhiteSpace는 건너뛴다
            Accept<QsWhitespaceToken>(await LexAsync(context), ref context);

            var stringElems = ImmutableArray.CreateBuilder<QsStringExpElement>();
            var exps = ImmutableArray.CreateBuilder<QsExp>();
            while (!Accept<QsEndOfCommandToken>(await LexAsync(context), ref context))
            {
                if (Accept<QsWhitespaceToken>(await LexAsync(context), ref context))
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
                var idToken = AcceptAndReturn<QsIdentifierToken>(await LexAsync(context), ref context);
                if (idToken != null)
                {
                    stringElems.Add(new QsExpStringExpElement(new QsIdentifierExp(idToken.Value)));
                    continue;
                }

                var textToken = AcceptAndReturn<QsTextToken>(await LexAsync(context), ref context);
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
            var varDeclResult = await ParseVarDeclStmtAsync(context);
            if (varDeclResult.HasValue)
                return Result(varDeclResult);
            
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

            while (!Accept<QsEndOfFileToken>(await LexAsync(context), ref context))
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