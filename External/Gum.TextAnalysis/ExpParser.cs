using Gum.LexicalAnalysis;
using Gum.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum
{
    using QsExpParseResult = QsParseResult<Exp>;
    using QsStringExpParseResult = QsParseResult<StringExp>;

    class ExpParser
    {
        Parser parser; // parentComponent
        Lexer lexer;

        public ExpParser(Parser parser, Lexer lexer)
        {
            this.parser = parser;
            this.lexer = lexer;
        }

        #region Utilities
        bool Accept<TToken>(LexResult lexResult, ref ParserContext context)
        {
            if (lexResult.HasValue && lexResult.Token is TToken)
            {
                context = context.Update(lexResult.Context);
                return true;
            }

            return false;
        }

        TToken? AcceptAndReturn<TToken>(LexResult lexResult, ref ParserContext context) where TToken : Token
        {
            if (lexResult.HasValue && lexResult.Token is TToken token)
            {
                context = context.Update(lexResult.Context);
                return token;
            }

            return null;
        }

        bool Peek<TToken>(LexResult lexResult) where TToken : Token
        {
            return lexResult.HasValue && lexResult.Token is TToken;
        }

        public delegate BinaryOpKind? AcceptBinaryOpKindFunc(LexResult result, ref ParserContext context);

        async ValueTask<QsExpParseResult> ParseLeftAssocBinaryOpExpAsync(
            ParserContext context,
            Func<ParserContext, ValueTask<QsExpParseResult>> ParseBaseExpAsync,
            (Token Token, BinaryOpKind OpKind)[] infos)
        {
            var expResult0 = await ParseBaseExpAsync(context);
            if (!expResult0.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult0.Context;
            Exp exp = expResult0.Elem;

            while (true)
            {
                BinaryOpKind? opKind = null;

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
                exp = new BinaryOpExp(opKind.Value, exp, expResult.Elem);
            }
        }
        #endregion        

        Exp? HandleUnaryMinusWithIntLiteral(UnaryOpKind kind, Exp exp)
        {
            if( kind == UnaryOpKind.Minus && exp is IntLiteralExp intLiteralExp)
            {
                return new IntLiteralExp(-intLiteralExp.Value);
            }

            return null;
        }

        #region Single
        async ValueTask<QsExpParseResult> ParseSingleExpAsync(ParserContext context)
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
        static (Token Token, UnaryOpKind OpKind)[] primaryInfos = new (Token Token, UnaryOpKind OpKind)[]
        {            
            (PlusPlusToken.Instance, UnaryOpKind.PostfixInc),
            (MinusMinusToken.Instance, UnaryOpKind.PostfixDec),
        };

        async ValueTask<QsParseResult<ImmutableArray<Exp>>> ParseCallArgs(ParserContext context)
        {
            if (!Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<ImmutableArray<Exp>>.Invalid;
            
            var args = ImmutableArray.CreateBuilder<Exp>();
            while (!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (0 < args.Count)
                    if (!Accept<CommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<ImmutableArray<Exp>>.Invalid;

                var argResult = await ParseExpAsync(context);
                if (!argResult.HasValue)
                    return QsParseResult<ImmutableArray<Exp>>.Invalid;

                context = argResult.Context;
                args.Add(argResult.Elem);
            }

            return new QsParseResult<ImmutableArray<Exp>>(args.ToImmutable(), context);
        }

        // TODO: 현재 Primary중 Postfix Unary만 구현했다.
        internal async ValueTask<QsExpParseResult> ParsePrimaryExpAsync(ParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(ParserContext context) => ParseSingleExpAsync(context);

            var expResult = await ParseBaseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;
            Exp exp = expResult.Elem;

            while (true)
            {
                // Unary일수도 있고, ()일수도 있다
                var lexResult = await lexer.LexNormalModeAsync(context.LexerContext, true);
                if (!lexResult.HasValue) break;

                (Token Token, UnaryOpKind OpKind)? primaryInfo = null;
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
                    exp = new UnaryOpExp(primaryInfo.Value.OpKind, exp);
                    continue;
                }

                // [ ... ]
                if (Accept<LBracketToken>(lexResult, ref context))
                {
                    var indexResult = await ParseExpAsync(context);
                    if (!indexResult.HasValue) return QsExpParseResult.Invalid;
                    context = indexResult.Context;

                    if (!Accept<RBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsExpParseResult.Invalid;

                    exp = new IndexerExp(exp, indexResult.Elem);
                    continue;
                }

                // . id (..., ...)
                if (Accept<DotToken>(lexResult, ref context))
                {
                    var idResult = AcceptAndReturn<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if (idResult == null) return QsExpParseResult.Invalid;

                    var memberCallArgsResult = await ParseCallArgs(context);
                    if (memberCallArgsResult.HasValue)
                    {
                        context = memberCallArgsResult.Context;
                        exp = new MemberCallExp(exp, idResult.Value, ImmutableArray<TypeExp>.Empty, memberCallArgsResult.Elem);
                        continue;
                    }
                    else
                    {
                        exp = new MemberExp(exp, idResult.Value, ImmutableArray<TypeExp>.Empty);
                        continue;
                    }
                }

                // (..., ... )
                var callArgsResult = await ParseCallArgs(context);
                if (callArgsResult.HasValue)
                {
                    context = callArgsResult.Context;
                    exp = new CallExp(exp, ImmutableArray<TypeExp>.Empty, callArgsResult.Elem);
                    continue;
                }

                break;
            }

            return new QsExpParseResult(exp, context);
        }
        #endregion

        #region Unary, Prefix Inc/Dec
        static (Token Token, UnaryOpKind OpKind)[] unaryInfos = new (Token Token, UnaryOpKind OpKind)[]
        {
            (MinusToken.Instance, UnaryOpKind.Minus),
            (ExclToken.Instance, UnaryOpKind.LogicalNot),
            (PlusPlusToken.Instance, UnaryOpKind.PrefixInc),
            (MinusMinusToken.Instance, UnaryOpKind.PrefixDec),
        };

        async ValueTask<QsExpParseResult> ParseUnaryExpAsync(ParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(ParserContext context) => ParsePrimaryExpAsync(context);

            UnaryOpKind? opKind = null;

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

                return new QsExpParseResult(new UnaryOpExp(opKind.Value, expResult.Elem), context);
            }
            else
            {
                return await ParseBaseExpAsync(context);
            }
        }
        #endregion

        #region Multiplicative, LeftAssoc
        static (Token Token, BinaryOpKind OpKind)[] multiplicativeInfos = new (Token Token, BinaryOpKind OpKind)[]
        {
            (StarToken.Instance, BinaryOpKind.Multiply),
            (SlashToken.Instance, BinaryOpKind.Divide),
            (PercentToken.Instance, BinaryOpKind.Modulo),
        };

        ValueTask<QsExpParseResult> ParseMultiplicativeExpAsync(ParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseUnaryExpAsync, multiplicativeInfos);
        }
        #endregion


        #region Additive, LeftAssoc
        static (Token Token, BinaryOpKind OpKind)[] additiveInfos = new (Token Token, BinaryOpKind OpKind)[]
        {
            (PlusToken.Instance, BinaryOpKind.Add),
            (MinusToken.Instance, BinaryOpKind.Subtract),
        };

        ValueTask<QsExpParseResult> ParseAdditiveExpAsync(ParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseMultiplicativeExpAsync, additiveInfos);
        }
        #endregion

        #region Test, LeftAssoc
        static (Token Token, BinaryOpKind OpKind)[] testInfos = new (Token Token, BinaryOpKind OpKind)[]
        {
            (GreaterThanEqualToken.Instance, BinaryOpKind.GreaterThanOrEqual),
            (LessThanEqualToken.Instance, BinaryOpKind.LessThanOrEqual),
            (LessThanToken.Instance, BinaryOpKind.LessThan),
            (GreaterThanToken.Instance, BinaryOpKind.GreaterThan),
        };

        ValueTask<QsExpParseResult> ParseTestExpAsync(ParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseAdditiveExpAsync, testInfos);
        }
        #endregion

        #region Equality, Left Assoc
        static (Token Token, BinaryOpKind OpKind)[] equalityInfos = new (Token Token, BinaryOpKind OpKind)[]
        {
            (EqualEqualToken.Instance, BinaryOpKind.Equal),
            (ExclEqualToken.Instance, BinaryOpKind.NotEqual),
        };

        ValueTask<QsExpParseResult> ParseEqualityExpAsync(ParserContext context)
        {
            return ParseLeftAssocBinaryOpExpAsync(context, ParseTestExpAsync, equalityInfos);
        }
        #endregion
        

        #region Assignment, Right Assoc
        async ValueTask<QsExpParseResult> ParseAssignExpAsync(ParserContext context)
        {
            ValueTask<QsExpParseResult> ParseBaseExpAsync(ParserContext context) => ParseEqualityExpAsync(context);

            // a => b를 파싱했을 때 a가 리턴되는 경우를 피하려면 순서상 람다가 먼저
            var lambdaResult = await ParseLambdaExpAsync(context);
            if (lambdaResult.HasValue)
                return new QsExpParseResult(lambdaResult.Elem, lambdaResult.Context);

            var expResult0 = await ParseBaseExpAsync(context);
            if (!expResult0.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult0.Context;

            if (!Accept<EqualToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return expResult0;

            var expResult1 = await ParseAssignExpAsync(context);
            if (!expResult1.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult1.Context;

            return new QsExpParseResult(new BinaryOpExp(BinaryOpKind.Assign, expResult0.Elem, expResult1.Elem), context);
        }

        #endregion

        #region LambdaExpression, Right Assoc
        async ValueTask<QsExpParseResult> ParseLambdaExpAsync(ParserContext context)
        {
            var parameters = ImmutableArray.CreateBuilder<LambdaExpParam>();

            // (), (a, b)
            // (int a)
            // a
            var idResult = AcceptAndReturn<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (idResult != null )
            {
                parameters.Add(new LambdaExpParam(null, idResult.Value));
            }
            else if (Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                while(!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    if (0 < parameters.Count)
                        if (!Accept<CommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                            return Invalid();

                    // id id or id
                    var firstIdResult = AcceptAndReturn<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if (firstIdResult == null)
                        return Invalid();

                    var secondIdResult = AcceptAndReturn<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
                    if( secondIdResult == null )
                        parameters.Add(new LambdaExpParam(null, firstIdResult.Value));
                    else
                        parameters.Add(new LambdaExpParam(new IdTypeExp(firstIdResult.Value), secondIdResult.Value));
                }
            }

            // =>
            if (!Accept<EqualGreaterThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            // exp => return exp;
            // { ... }
            Stmt body;
            if (Peek<LBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true)))
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

                body = new ReturnStmt(expBodyResult.Elem);
            }

            return new QsExpParseResult(new LambdaExp(parameters.ToImmutable(), body), context);

            static QsExpParseResult Invalid() => QsExpParseResult.Invalid;
        }
        #endregion

        public ValueTask<QsExpParseResult> ParseExpAsync(ParserContext context)
        {   
            return ParseAssignExpAsync(context);
        }
        
        async ValueTask<QsExpParseResult> ParseParenExpAsync(ParserContext context)
        {
            if (!Accept<LParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;
            
            var expResult = await ParseExpAsync(context);
            if (!expResult.HasValue)
                return QsExpParseResult.Invalid;

            context = expResult.Context;

            if (!Accept<RParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;

            return new QsExpParseResult(expResult.Elem, context);
        }

        async ValueTask<QsExpParseResult> ParseBoolLiteralExpAsync(ParserContext context)
        {
            var boolResult = AcceptAndReturn<BoolToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (boolResult != null)
                return new QsExpParseResult(new BoolLiteralExp(boolResult.Value), context);

            return QsExpParseResult.Invalid;
        }

        async ValueTask<QsExpParseResult> ParseIntLiteralExpAsync(ParserContext context)
        {
            var intResult = AcceptAndReturn<IntToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (intResult != null)
                return new QsExpParseResult(new IntLiteralExp(intResult.Value), context);

            return QsExpParseResult.Invalid;
        }

        // 스트링 파싱
        public async ValueTask<QsStringExpParseResult> ParseStringExpAsync(ParserContext context)
        {
            if (!Accept<DoubleQuoteToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsStringExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<StringExpElement>();
            while (!Accept<DoubleQuoteToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context))
            {
                var textToken = AcceptAndReturn<TextToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if (textToken != null)
                {
                    elems.Add(new TextStringExpElement(textToken.Text));
                    continue;
                }

                var idToken = AcceptAndReturn<IdentifierToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context);
                if (idToken != null)
                {
                    elems.Add(new ExpStringExpElement(new IdentifierExp(idToken.Value)));
                    continue;
                }

                // ${
                if (Accept<DollarLBraceToken>(await lexer.LexStringModeAsync(context.LexerContext), ref context))
                {
                    var expResult = await ParseExpAsync(context); // TODO: EndInnerExpToken 일때 빠져나와야 한다는 표시를 해줘야 한다
                    if (!expResult.HasValue)
                        return QsStringExpParseResult.Invalid;

                    context = expResult.Context;

                    if (!Accept<RBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsStringExpParseResult.Invalid;

                    elems.Add(new ExpStringExpElement(expResult.Elem));
                    continue;
                }

                // 나머지는 에러
                return QsStringExpParseResult.Invalid;
            }

            return new QsStringExpParseResult(new StringExp(elems.ToImmutable()), context);
        }

        public async ValueTask<QsExpParseResult> ParseListExpAsync(ParserContext context)
        {
            if (!Accept<LBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsExpParseResult.Invalid;

            var elems = ImmutableArray.CreateBuilder<Exp>();
            while (!Accept<RBracketToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (0 < elems.Count)
                    if (!Accept<CommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsExpParseResult.Invalid;

                var elemResult = await ParseExpAsync(context);
                if (!elemResult.HasValue) return QsExpParseResult.Invalid;
                context = elemResult.Context;

                elems.Add(elemResult.Elem);
            }

            return new QsExpParseResult(new ListExp(null, elems.ToImmutable()), context);
        }

        async ValueTask<QsExpParseResult> ParseIdentifierExpAsync(ParserContext context)
        {
            var idToken = AcceptAndReturn<IdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (idToken != null)
                return new QsExpParseResult(new IdentifierExp(idToken.Value), context);

            return QsExpParseResult.Invalid;
        }
    }
}
