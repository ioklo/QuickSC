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

        public static QsParserContext Make(QsLexerContext lexerContext)
        {
            return new QsParserContext(lexerContext);
        }

        private QsParserContext(QsLexerContext lexerContext)
        {
            LexerContext = lexerContext;
        }

        public QsParserContext Update(QsLexerContext newContext)
        {
            return new QsParserContext(newContext);
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

        //public async ValueTask<(QsExp Exp, QsBufferPosition NextPos)?> ParseCommandStatementCommandExpAsync(QsBufferPosition pos)
        //{
        //    var cmdTokenResult = await lexer.LexAsync(GetNextCommandTokenAsync(pos);

        //    if (cmdTokenResult.HasValue)
        //    {
        //        if (cmdTokenResult.Value.Token is QsStringCommandToken stringCmdToken) // quoted string 검사
        //            return (MakeStringExp(stringCmdToken.Token), cmdTokenResult.Value.NextPos);
        //        else if (cmdTokenResult.Value.Token is QsIdentifierCommandToken idCmdToken)
        //            return (new QsIdentifierExp(idCmdToken.Token.Value), cmdTokenResult.Value.NextPos);
        //    }

        //    return null;
        //}

        //public async ValueTask<(QsExp Exp, QsBufferPosition NextPos)?> ParseCommandStatementArgExpAsync(QsBufferPosition pos)
        //{
        //    var cmdTokenResult = await commandLexer.GetNextArgTokenAsync(pos);
        //    if (cmdTokenResult.HasValue && cmdTokenResult.Value.Token is QsStringCommandArgToken stringCmdArgToken) // quoted string 검사
        //    {
        //        return (MakeStringExp(stringCmdArgToken.Token), cmdTokenResult.Value.NextPos);
        //    }

        //    return null;
        //}

        // 커맨드란
        async ValueTask<QsParseResult<QsCommandStatement>> ParseCommandStatementAsync(QsParserContext context)
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

                return QsParseResult<QsCommandStatement>.Invalid;
            }

            // 지금까지 모아놓은거 또 flush
            if (0 < stringElems.Count)
            {
                exps.Add(new QsStringExp(stringElems.ToImmutable()));
                stringElems.Clear();
            }

            if (exps.Count == 0) return QsParseResult<QsCommandStatement>.Invalid;

            return new QsParseResult<QsCommandStatement>(new QsCommandStatement(exps[0], exps.Skip(1).ToImmutableArray()), context);
        }

        public async ValueTask<QsParseResult<QsStatement>> ParseStatementAsync(QsParserContext context)
        {
            var cmdResult = await ParseCommandStatementAsync(context);
            if (cmdResult.HasValue) 
                return new QsParseResult<QsStatement>(cmdResult.Elem, cmdResult.Context);

            throw new NotImplementedException();
        }

        async ValueTask<QsParseResult<QsScriptElement>> ParseScriptElementAsync(QsParserContext context)
        {
            var stmtResult = await ParseStatementAsync(context);
            if (stmtResult.HasValue) 
                return new QsParseResult<QsScriptElement>(new QsStatementScriptElement(stmtResult.Elem), stmtResult.Context);

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