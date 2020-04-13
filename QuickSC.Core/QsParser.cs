using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    using QsExpParseResult = QsParseResult<QsExp>;

    struct QsParserContext
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

    struct QsParseResult<TSyntaxElem>
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

    class QsParser
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
                return stringExpResult;

            var idExpResult = await ParseIdentifierExpAsync(context);
            if (idExpResult.HasValue)
                return idExpResult;

            return QsExpParseResult.Invalid;
        }
        
        // 스트링 파싱
        async ValueTask<QsExpParseResult> ParseStringExpAsync(QsParserContext context)
        {
            if (!Accept<QsBeginStringToken>(await LexAsync(context), ref context))
                return QsExpParseResult.Invalid;

            var elems = new List<QsStringExpElement>();
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
                        return QsExpParseResult.Invalid;

                    context = expResult.Context;

                    if (!Accept<QsEndInnerExpToken>(await LexAsync(context), ref context))
                        return QsExpParseResult.Invalid;

                    elems.Add(new QsExpStringExpElement(expResult.Elem));
                    continue;
                }

                // 나머지는 에러
                return QsExpParseResult.Invalid;
            }

            return new QsExpParseResult(new QsStringExp(elems), context);
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

        //public async ValueTask<(QsCommandStatement Stmt, QsBufferPosition NextPos)?> ParseCommandStatementAsync(QsBufferPosition pos)
        //{
        //    var commandExpResult = await ParseCommandStatementCommandExpAsync(pos);
        //    if (!commandExpResult.HasValue)
        //        return null;

        //    var argExps = new List<QsExp>();

        //    var curPos = commandExpResult.Value.NextPos;

        //    while (!curPos.IsReachEnd())
        //    {
        //        // 끝 체크가 너무 장황하다 ㅋ
        //        var cmdArgTokenResult = await commandLexer.GetNextArgTokenAsync(curPos);
        //        if (cmdArgTokenResult.HasValue && cmdArgTokenResult.Value.Token is QsEndOfCommandArgToken)
        //            break;

        //        var argExpResult = await ParseCommandStatementArgExpAsync(curPos);
        //        if (!argExpResult.HasValue)
        //            throw new InvalidOperationException();

        //        argExps.Add(argExpResult.Value.Exp);
        //        curPos = argExpResult.Value.NextPos;
        //    }

        //    return (new QsCommandStatement(commandExpResult.Value.Exp, argExps), curPos);
        //}

        //public async ValueTask<(QsStatement Stmt, QsBufferPosition NextPos)?> ParseStatementAsync(QsBufferPosition pos)
        //{
        //    var cmdResult = await ParseCommandStatementAsync(pos);
        //    if (cmdResult.HasValue) return cmdResult;

        //    throw new NotImplementedException();
        //}

        //public async ValueTask<(QsScriptElement Elem, QsBufferPosition NextPos)?> ParseScriptElementAsync(QsBufferPosition pos)
        //{
        //    var stmtResult = await ParseStatementAsync(pos);
        //    if (stmtResult.HasValue) return (new QsStatementScriptElement(stmtResult.Value.Stmt), stmtResult.Value.NextPos);

        //    return null;
        //}

        //public async ValueTask<(QsScript Script, QsBufferPosition NextPos)?> ParseScriptAsync(QsBufferPosition pos)
        //{
        //    var elems = new List<QsScriptElement>();

        //    while (!pos.IsReachEnd())
        //    {
        //        // 끝인지 검사
        //        var tokenResult = await lexer.LexAsync(pos);
        //        if (tokenResult.HasValue && tokenResult.Value.Token.IsSingleToken(QsSimpleTokenKind.EndOfFile))
        //            break;

        //        var elemResult = await ParseScriptElementAsync(pos);
        //        if (!elemResult.HasValue) return null;

        //        elems.Add(elemResult.Value.Elem);
        //        pos = elemResult.Value.NextPos;
        //    }

        //    return (new QsScript(elems), pos);
        //}
    }
}