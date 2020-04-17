using QuickSC.Syntax;
using QuickSC.Token;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSC
{
    using QsExpParseResult = QsParseResult<QsExp>;
    using QsStringExpParseResult = QsParseResult<QsStringExp>;

    class QsExpParser
    {
        QsLexer lexer;

        public QsExpParser(QsLexer lexer)
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

        public delegate QsBinaryOpKind? AcceptBinaryOpKindFunc(QsLexResult result, ref QsParserContext context);

        async ValueTask<QsExpParseResult> ParseLeftAssocBinaryOpExpAsync(
            QsParserContext context,
            Func<QsParserContext, ValueTask<QsExpParseResult>> ParseBaseExpAsync,
            (QsToken Token, QsBinaryOpKind OpKind)[] infos)
        {
            var expResult0 = await ParseBaseExpAsync(context);
            if (!expResult0.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult0.Context;
            QsExp exp = expResult0.Elem;

            while (true)
            {
                QsBinaryOpKind? opKind = null;

                var lexResult = await lexer.LexNormalModeAsync(context.LexerContext);
                if (lexResult.HasValue)
                {
                    foreach (var info in infos)
                    {
                        if (info.Token == lexResult.Token)
                        {
                            opKind = info.OpKind;
                            context = context.Update(lexResult.Context);
                            break;
                        }
                    }
                }

                if (!opKind.HasValue)
                    return new QsExpParseResult(exp, context);

                var expResult = await ParseBaseExpAsync(context);
                if (!expResult.HasValue)
                    return QsExpParseResult.Invalid;

                context = expResult.Context;

                // Fold
                exp = new QsBinaryOpExp(opKind.Value, exp, expResult.Elem);
            }
        }
        #endregion        

        #region Single
        async ValueTask<QsExpParseResult> ParseSingleExpAsync(QsParserContext context)
        {
            var parenExpResult = await ParseParenExpAsync(context);
            if (parenExpResult.HasValue)
                return parenExpResult;

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

        #endregion

        
        #region Primary, Postfix Inc/Dec
        static (QsToken Token, QsUnaryOpKind OpKind)[] primaryInfos = new (QsToken Token, QsUnaryOpKind OpKind)[]
        {            
            (QsPlusPlusToken.Instance, QsUnaryOpKind.PostfixInc),
            (QsMinusMinusToken.Instance, QsUnaryOpKind.PostfixDec),
        };

        // TODO: 현재 Primary중 Postfix Unary만 구현했다.
        internal async ValueTask<QsExpParseResult> ParsePrimaryExpAsync(QsParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(QsParserContext context) => ParseSingleExpAsync(context);

            var expResult = await ParseBaseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;

            QsUnaryOpKind? opKind = null;

            // NOTICE: Postfix Unary는 중첩시키지 않고 한개만 쓰기로 한다...
            var lexResult = await lexer.LexNormalModeAsync(context.LexerContext);
            if (lexResult.HasValue)
            {
                foreach (var info in primaryInfos)
                {
                    if (info.Token == lexResult.Token)
                    {
                        opKind = info.OpKind;
                        context = context.Update(lexResult.Context);
                        break;
                    }
                }
            }

            if (opKind.HasValue)
            {
                // TODO: unaryMinus intLiteral은 intLiteral로 변경
                return new QsExpParseResult(new QsUnaryOpExp(opKind.Value, expResult.Elem), context);
            }
            else
            {
                return expResult;
            }
        }
        #endregion

        #region Unary, Prefix Inc/Dec
        static (QsToken Token, QsUnaryOpKind OpKind)[] unaryInfos = new (QsToken Token, QsUnaryOpKind OpKind)[]
        {
            (QsExclToken.Instance, QsUnaryOpKind.LogicalNot),
            (QsPlusPlusToken.Instance, QsUnaryOpKind.PrefixInc),
            (QsMinusMinusToken.Instance, QsUnaryOpKind.PrefixDec),
        };

        async ValueTask<QsExpParseResult> ParseUnaryExpAsync(QsParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(QsParserContext context) => ParsePrimaryExpAsync(context);

            QsUnaryOpKind? opKind = null;

            var lexResult = await lexer.LexNormalModeAsync(context.LexerContext);
            if (lexResult.HasValue)
            {
                foreach (var info in unaryInfos)
                {
                    if (info.Token == lexResult.Token)
                    {
                        opKind = info.OpKind;
                        context = context.Update(lexResult.Context);
                        break;
                    }
                }
            }

            if (opKind.HasValue)
            {
                var expResult = await ParseUnaryExpAsync(context);
                if (!expResult.HasValue)
                    return QsExpParseResult.Invalid;

                context = expResult.Context;

                // TODO: unaryMinus intLiteral은 intLiteral로 변경
                return new QsExpParseResult(new QsUnaryOpExp(opKind.Value, expResult.Elem), context);
            }
            else
            {
                return await ParseBaseExpAsync(context);
            }
        }
        #endregion

        #region Multiplicative, LeftAssoc
        static (QsToken Token, QsBinaryOpKind OpKind)[] multiplicativeInfos = new (QsToken Token, QsBinaryOpKind OpKind)[]
        {
            (QsStarToken.Instance, QsBinaryOpKind.Multiply),
            (QsSlashToken.Instance, QsBinaryOpKind.Divide),
            (QsPercentToken.Instance, QsBinaryOpKind.Modulo),
        };

        ValueTask<QsExpParseResult> ParseMultiplicativeExpAsync(QsParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseUnaryExpAsync, multiplicativeInfos);
        }
        #endregion


        #region Additive, LeftAssoc
        static (QsToken Token, QsBinaryOpKind OpKind)[] additiveInfos = new (QsToken Token, QsBinaryOpKind OpKind)[]
        {
            (QsPlusToken.Instance, QsBinaryOpKind.Add),
            (QsMinusToken.Instance, QsBinaryOpKind.Subtract),
        };

        ValueTask<QsExpParseResult> ParseAdditiveExpAsync(QsParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseMultiplicativeExpAsync, additiveInfos);
        }
        #endregion

        #region Test, LeftAssoc
        static (QsToken Token, QsBinaryOpKind OpKind)[] testInfos = new (QsToken Token, QsBinaryOpKind OpKind)[]
        {
            (QsGreaterThanEqualToken.Instance, QsBinaryOpKind.GreaterThanOrEqual),
            (QsLessThanEqualToken.Instance, QsBinaryOpKind.LessThanOrEqual),
            (QsLessThanToken.Instance, QsBinaryOpKind.LessThan),
            (QsGreaterThanToken.Instance, QsBinaryOpKind.GreaterThan),
        };

        ValueTask<QsExpParseResult> ParseTestExpAsync(QsParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseAdditiveExpAsync, testInfos);
        }
        #endregion

        #region Equality, Left Assoc
        static (QsToken Token, QsBinaryOpKind OpKind)[] equalityInfos = new (QsToken Token, QsBinaryOpKind OpKind)[]
        {
            (QsEqualEqualToken.Instance, QsBinaryOpKind.Equal),
            (QsExclEqualToken.Instance, QsBinaryOpKind.NotEqual),
        };

        ValueTask<QsExpParseResult> ParseEqualityExpAsync(QsParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseTestExpAsync, equalityInfos);
        }
        #endregion
        

        #region Assignment, Right Assoc
        async ValueTask<QsExpParseResult> ParseAssignExpAsync(QsParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(QsParserContext context) => ParseEqualityExpAsync(context);

            var expResult0 = await ParseBaseExpAsync(context);
            if (!expResult0.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult0.Context;

            if (!Accept<QsEqualToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return expResult0;

            var expResult1 = await ParseAssignExpAsync(context);
            if (!expResult1.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult1.Context;

            return new QsExpParseResult(new QsBinaryOpExp(QsBinaryOpKind.Assign, expResult0.Elem, expResult1.Elem), context);
        }

        #endregion

        public ValueTask<QsExpParseResult> ParseExpAsync(QsParserContext context)
        {
            return ParseAssignExpAsync(context);
        }

        async ValueTask<QsExpParseResult> ParseParenExpAsync(QsParserContext context)
        {
            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsExpParseResult.Invalid;
            
            var expResult = await ParseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsExpParseResult.Invalid;

            return new QsExpParseResult(expResult.Elem, context);
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
        public async ValueTask<QsStringExpParseResult> ParseStringExpAsync(QsParserContext context)
        {
            if (!Accept<QsDoubleQuoteToken>(await lexer.LexNormalModeAsync(context.LexerContext), ref context))
                return QsStringExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<QsStringExpElement>();
            while (!Accept<QsDoubleQuoteToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context))
            {
                var textToken = AcceptAndReturn<QsTextToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if (textToken != null)
                {
                    elems.Add(new QsTextStringExpElement(textToken.Text));
                    continue;
                }

                var idToken = AcceptAndReturn<QsIdentifierToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if (idToken != null)
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
    }
}
