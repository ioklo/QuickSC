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
        QsParser parser; // parentComponent
        QsLexer lexer;

        public QsExpParser(QsParser parser, QsLexer lexer)
        {
            this.parser = parser;
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

                var lexResult = await lexer.LexNormalModeAsync(context.LexerContext, true);
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

        QsExp? HandleUnaryMinusWithIntLiteral(QsUnaryOpKind kind, QsExp exp)
        {
            if( kind == QsUnaryOpKind.Minus && exp is QsIntLiteralExp intLiteralExp)
            {
                return new QsIntLiteralExp(-intLiteralExp.Value);
            }

            return null;
        }

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

            var listExpResult = await ParseListExpAsync(context);
            if (listExpResult.HasValue)
                return new QsExpParseResult(listExpResult.Elem, listExpResult.Context);

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

        async ValueTask<QsParseResult<ImmutableArray<QsExp>>> ParseCallArgs(QsParserContext context)
        {
            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ImmutableArray<QsExp>>.Invalid;
            
            var args = ImmutableArray.CreateBuilder<QsExp>();
            while (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (0 < args.Count)
                    if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<ImmutableArray<QsExp>>.Invalid;

                var argResult = await ParseExpAsync(context);
                if (!argResult.HasValue)
                    return QsParseResult<ImmutableArray<QsExp>>.Invalid;

                context = argResult.Context;
                args.Add(argResult.Elem);
            }

            return new QsParseResult<ImmutableArray<QsExp>>(args.ToImmutable(), context);
        }

        // TODO: 현재 Primary중 Postfix Unary만 구현했다.
        internal async ValueTask<QsExpParseResult> ParsePrimaryExpAsync(QsParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(QsParserContext context) => ParseSingleExpAsync(context);

            var expResult = await ParseBaseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;
            QsExp exp = expResult.Elem;

            while (true)
            {
                // Unary일수도 있고, ()일수도 있다
                var lexResult = await lexer.LexNormalModeAsync(context.LexerContext, true);
                if (!lexResult.HasValue) break;

                (QsToken Token, QsUnaryOpKind OpKind)? primaryInfo = null;
                foreach (var info in primaryInfos)
                    if (info.Token == lexResult.Token)
                    {
                        // TODO: postfix++이 두번 이상 나타나지 않도록 한다
                        primaryInfo = info;
                        break;
                    }

                if (primaryInfo.HasValue)
                {
                    context = context.Update(lexResult.Context);

                    // Fold
                    exp = new QsUnaryOpExp(primaryInfo.Value.OpKind, exp);
                    continue;
                }

                // [ ... ]
                if (Accept<QsLBracketToken>(lexResult, ref context))
                {
                    var indexResult = await ParseExpAsync(context);
                    if (!indexResult.HasValue) return QsExpParseResult.Invalid;
                    context = indexResult.Context;

                    if (!Accept<QsRBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsExpParseResult.Invalid;

                    exp = new QsMemberCallExp(exp, new QsMemberFuncId(QsMemberFuncKind.Indexer), indexResult.Elem);
                    continue;
                }

                // . id (..., ...)
                if (Accept<QsDotToken>(lexResult, ref context))
                {
                    var idResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if (idResult == null) return QsExpParseResult.Invalid;

                    var memberCallArgsResult = await ParseCallArgs(context);
                    if (memberCallArgsResult.HasValue)
                    {
                        context = memberCallArgsResult.Context;
                        exp = new QsMemberCallExp(exp, new QsMemberFuncId(idResult.Value), memberCallArgsResult.Elem);
                        continue;
                    }
                    else
                    {
                        exp = new QsMemberExp(exp, idResult.Value);
                        continue;
                    }
                }

                // (..., ... )
                var callArgsResult = await ParseCallArgs(context);
                if (callArgsResult.HasValue)
                {
                    context = callArgsResult.Context;
                    exp = new QsCallExp(new QsExpCallExpCallable(exp), callArgsResult.Elem);
                    continue;
                }

                break;
            }

            return new QsExpParseResult(exp, context);
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

            var lexResult = await lexer.LexNormalModeAsync(context.LexerContext, true);
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

                var handledExp = HandleUnaryMinusWithIntLiteral(opKind.Value, expResult.Elem);
                if (handledExp != null)                
                    return new QsExpParseResult(handledExp, context);

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

            // a => b를 파싱했을 때 a가 리턴되는 경우를 피하려면 순서상 람다가 먼저
            var lambdaResult = await ParseLambdaExpAsync(context);
            if (lambdaResult.HasValue)
                return new QsExpParseResult(lambdaResult.Elem, lambdaResult.Context);

            var expResult0 = await ParseBaseExpAsync(context);
            if (!expResult0.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult0.Context;

            if (!Accept<QsEqualToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return expResult0;

            var expResult1 = await ParseAssignExpAsync(context);
            if (!expResult1.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult1.Context;

            return new QsExpParseResult(new QsBinaryOpExp(QsBinaryOpKind.Assign, expResult0.Elem, expResult1.Elem), context);
        }

        #endregion

        #region LambdaExpression, Right Assoc
        async ValueTask<QsExpParseResult> ParseLambdaExpAsync(QsParserContext context)
        {
            var parameters = ImmutableArray.CreateBuilder<QsLambdaExpParam>();

            // (), (a, b)
            // (int a)
            // a
            var idResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (idResult != null )
            {
                parameters.Add(new QsLambdaExpParam(null, idResult.Value));
            }
            else if (Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                while(!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    if (0 < parameters.Count)
                        if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                            return Invalid();

                    // id id or id
                    var firstIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if (firstIdResult == null)
                        return Invalid();

                    var secondIdResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if( secondIdResult == null )
                        parameters.Add(new QsLambdaExpParam(null, firstIdResult.Value));
                    else
                        parameters.Add(new QsLambdaExpParam(new QsTypeIdExp(firstIdResult.Value), secondIdResult.Value));
                }
            }

            // =>
            if (!Accept<QsEqualGreaterThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // exp => return exp;
            // { ... }
            QsStmt body;
            if (Peek<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true)))
            {
                var stmtBodyResult = await parser.ParseStmtAsync(context);
                if (!stmtBodyResult.HasValue)
                    return Invalid();
                context = stmtBodyResult.Context;

                body = stmtBodyResult.Elem;
            }
            else
            {
                var expBodyResult = await parser.ParseExpAsync(context);
                if (!expBodyResult.HasValue)
                    return Invalid();
                context = expBodyResult.Context;

                body = new QsReturnStmt(expBodyResult.Elem);
            }

            return new QsExpParseResult(new QsLambdaExp(parameters.ToImmutable(), body), context);

            static QsExpParseResult Invalid() => QsExpParseResult.Invalid;
        }
        #endregion

        public ValueTask<QsExpParseResult> ParseExpAsync(QsParserContext context)
        {   
            return ParseAssignExpAsync(context);
        }
        
        async ValueTask<QsExpParseResult> ParseParenExpAsync(QsParserContext context)
        {
            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;
            
            var expResult = await ParseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;

            if (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;

            return new QsExpParseResult(expResult.Elem, context);
        }

        async ValueTask<QsExpParseResult> ParseBoolLiteralExpAsync(QsParserContext context)
        {
            var boolResult = AcceptAndReturn<QsBoolToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (boolResult != null)
                return new QsExpParseResult(new QsBoolLiteralExp(boolResult.Value), context);

            return QsExpParseResult.Invalid;
        }

        async ValueTask<QsExpParseResult> ParseIntLiteralExpAsync(QsParserContext context)
        {
            var intResult = AcceptAndReturn<QsIntToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (intResult != null)
                return new QsExpParseResult(new QsIntLiteralExp(intResult.Value), context);

            return QsExpParseResult.Invalid;
        }

        // 스트링 파싱
        public async ValueTask<QsStringExpParseResult> ParseStringExpAsync(QsParserContext context)
        {
            if (!Accept<QsDoubleQuoteToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
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

                    if (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsStringExpParseResult.Invalid;

                    elems.Add(new QsExpStringExpElement(expResult.Elem));
                    continue;
                }

                // 나머지는 에러
                return QsStringExpParseResult.Invalid;
            }

            return new QsStringExpParseResult(new QsStringExp(elems.ToImmutable()), context);
        }

        public async ValueTask<QsExpParseResult> ParseListExpAsync(QsParserContext context)
        {
            if (!Accept<QsLBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<QsExp>();
            while (!Accept<QsRBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (0 < elems.Count)
                    if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsExpParseResult.Invalid;

                var elemResult = await ParseExpAsync(context);
                if (!elemResult.HasValue) return QsExpParseResult.Invalid;
                context = elemResult.Context;

                elems.Add(elemResult.Elem);
            }

            return new QsExpParseResult(new QsListExp(elems.ToImmutable()), context);
        }

        async ValueTask<QsExpParseResult> ParseIdentifierExpAsync(QsParserContext context)
        {
            var idToken = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (idToken != null)
                return new QsExpParseResult(new QsIdentifierExp(idToken.Value), context);

            return QsExpParseResult.Invalid;
        }
    }
}
