using QuickSC.Syntax;
using QuickSC.Token;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace QuickSC
{
    // 백트래킹을 하는데는 immuatable이 편하기 때문에, Immutable로 간다
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
        internal QsExpParser expParser;
        internal QsStmtParser stmtParser;

        public QsParser(QsLexer lexer)
        {
            this.lexer = lexer;
            expParser = new QsExpParser(this, lexer);
            stmtParser = new QsStmtParser(this, lexer);
        }

        public async ValueTask<QsScript?> ParseScriptAsync(string input)
        {
            var buffer = new QsBuffer(new StringReader(input));
            var pos = await buffer.MakePosition().NextAsync();
            var context = QsParserContext.Make(QsLexerContext.Make(pos));

            var scriptResult = await ParseScriptAsync(context);
            return scriptResult.HasValue ? scriptResult.Elem : null;
        }

        public ValueTask<QsParseResult<QsExp>> ParseExpAsync(QsParserContext context)
        {
            return expParser.ParseExpAsync(context);
        }

        public ValueTask<QsParseResult<QsStmt>> ParseStmtAsync(QsParserContext context)
        {
            return stmtParser.ParseStmtAsync(context);
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

        bool Accept<TToken>(QsLexResult lexResult, ref QsParserContext context, [NotNullWhen(returnValue:true)] out TToken? token) where TToken : QsToken
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

        bool Parse<TSyntaxElem>(
            QsParseResult<TSyntaxElem> parseResult, 
            ref QsParserContext context, 
            [NotNullWhen(returnValue: true)] out TSyntaxElem? elem) where TSyntaxElem : class
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

        async ValueTask<QsParseResult<QsTypeExp>> ParseTypeIdExpAsync(QsParserContext context)
        {
            if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var idToken))
                return QsParseResult<QsTypeExp>.Invalid;

            var typeArgsBuilder = ImmutableArray.CreateBuilder<QsTypeExp>();
            if (Accept<QsLessThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                while(!Accept<QsGreaterThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    if (0 < typeArgsBuilder.Count)
                        if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                            return QsParseResult<QsTypeExp>.Invalid;

                    if (!Parse(await ParseTypeExpAsync(context), ref context, out var typeArg))
                        return QsParseResult<QsTypeExp>.Invalid;

                    typeArgsBuilder.Add(typeArg);
                }

            return new QsParseResult<QsTypeExp>(new QsIdTypeExp(idToken.Value, typeArgsBuilder.ToImmutable()), context);
        }

        async ValueTask<QsParseResult<QsTypeExp>> ParsePrimaryTypeExpAsync(QsParserContext context)
        {
            if (!Parse(await ParseTypeIdExpAsync(context), ref context, out var typeIdExp))
                return QsParseResult<QsTypeExp>.Invalid;

            QsTypeExp exp = typeIdExp;
            while(true)
            {
                var lexResult = await lexer.LexNormalModeAsync(context.LexerContext, true);

                // . id (..., ...)
                if (Accept<QsDotToken>(lexResult, ref context))
                {
                    if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var memberName))
                        return QsParseResult<QsTypeExp>.Invalid;

                    // TODO: typeApp(T.S<>) 처리도 추가
                    exp = new QsMemberTypeExp(exp, memberName.Value, ImmutableArray<QsTypeExp>.Empty);
                    continue;
                }

                break;
            }

            return new QsParseResult<QsTypeExp>(exp, context);
        }

        public ValueTask<QsParseResult<QsTypeExp>> ParseTypeExpAsync(QsParserContext context)
        {
            return ParsePrimaryTypeExpAsync(context);
        }

        // int a, 
        async ValueTask<QsParseResult<(QsTypeAndName FuncDeclParam, bool bVariadic)>> ParseFuncDeclParamAsync(QsParserContext context)
        {
            var bVariadic = Accept<QsParamsToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);

            var typeExpResult = await ParseTypeExpAsync(context);
            if (!typeExpResult.HasValue)
                return QsParseResult<(QsTypeAndName, bool)>.Invalid;

            context = typeExpResult.Context;

            if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var name))
                return QsParseResult<(QsTypeAndName, bool)>.Invalid;

            return new QsParseResult<(QsTypeAndName, bool)>((new QsTypeAndName(typeExpResult.Elem, name.Value), bVariadic), context);
        }

        internal async ValueTask<QsParseResult<QsFuncDecl>> ParseFuncDeclAsync(QsParserContext context)
        {
            // <SEQ> <RetTypeName> <FuncName> <LPAREN> <ARGS> <RPAREN>
            // LBRACE>
            // [Stmt]
            // <RBRACE>            

            QsFuncKind funcKind;
            if (Accept<QsSeqToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                funcKind = QsFuncKind.Sequence;
            else
                funcKind = QsFuncKind.Normal;

            var retTypeResult = await ParseTypeExpAsync(context);
            if (!retTypeResult.HasValue)
                return Invalid();
            context = retTypeResult.Context;

            if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var funcName))            
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            var funcDeclParams = ImmutableArray.CreateBuilder<QsTypeAndName>();
            int? variadicParamIndex = null;
            while (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (funcDeclParams.Count != 0)
                    if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return Invalid();

                var funcDeclParam = await ParseFuncDeclParamAsync(context);
                if (!funcDeclParam.HasValue)
                    return Invalid();

                if (funcDeclParam.Elem.bVariadic)
                    variadicParamIndex = funcDeclParams.Count;

                funcDeclParams.Add(funcDeclParam.Elem.FuncDeclParam);
                context = funcDeclParam.Context;                
            }

            var blockStmtResult = await stmtParser.ParseBlockStmtAsync(context);
            if (!blockStmtResult.HasValue)
                return Invalid();

            context = blockStmtResult.Context;

            return new QsParseResult<QsFuncDecl>(
                new QsFuncDecl(
                    funcKind, 
                    retTypeResult.Elem, 
                    funcName.Value,
                    ImmutableArray<string>.Empty,
                    funcDeclParams.ToImmutable(), 
                    variadicParamIndex, 
                    blockStmtResult.Elem), 
                context);

            static QsParseResult<QsFuncDecl> Invalid() => QsParseResult<QsFuncDecl>.Invalid;
        }

        internal async ValueTask<QsParseResult<QsEnumDecl>> ParseEnumDeclAsync(QsParserContext context)
        {
            // enum E<T1, T2> { a , b () } 
            if (!Accept<QsEnumToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsEnumDecl>.Invalid;

            if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var enumName))
                return QsParseResult<QsEnumDecl>.Invalid;
            
            // typeParams
            var typeParamsBuilder = ImmutableArray.CreateBuilder<string>();
            if (Accept<QsLessThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                while(!Accept<QsGreaterThanToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    if (0 < typeParamsBuilder.Count)
                        if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                            return QsParseResult<QsEnumDecl>.Invalid;                    

                    // 변수 이름만 받을 것이므로 TypeExp가 아니라 Identifier여야 한다
                    if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var typeParam))
                        return QsParseResult<QsEnumDecl>.Invalid;

                    typeParamsBuilder.Add(typeParam.Value);
                }
            }

            if (!Accept<QsLBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return QsParseResult<QsEnumDecl>.Invalid;

            var elements = ImmutableArray.CreateBuilder<QsEnumDeclElement>();
            while (!Accept<QsRBraceToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
            {
                if (0 < elements.Count)
                    if (!Accept<QsCommaToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                        return QsParseResult<QsEnumDecl>.Invalid;

                if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var elemName))
                    return QsParseResult<QsEnumDecl>.Invalid;

                var parameters = ImmutableArray.CreateBuilder<QsTypeAndName>();
                if (Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                {
                    while (!Accept<QsRParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                    {
                        if (!Parse(await ParseTypeExpAsync(context), ref context, out var typeExp))
                            return QsParseResult<QsEnumDecl>.Invalid;

                        if (!Accept<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context, out var paramName))
                            return QsParseResult<QsEnumDecl>.Invalid;

                        parameters.Add(new QsTypeAndName(typeExp!, paramName.Value));
                    }
                }

                elements.Add(new QsEnumDeclElement(elemName.Value, parameters.ToImmutable()));
            }

            return new QsParseResult<QsEnumDecl>(new QsEnumDecl(enumName.Value, typeParamsBuilder.ToImmutable(), elements.ToImmutable()), context);
        }

        async ValueTask<QsParseResult<QsScriptElement>> ParseScriptElementAsync(QsParserContext context)
        {
            if (Parse(await ParseEnumDeclAsync(context), ref context, out var enumDecl))
                return new QsParseResult<QsScriptElement>(new QsEnumDeclScriptElement(enumDecl!), context);

            if (Parse(await ParseFuncDeclAsync(context), ref context, out var funcDecl))
                return new QsParseResult<QsScriptElement>(new QsFuncDeclScriptElement(funcDecl!), context);

            if (Parse(await stmtParser.ParseStmtAsync(context), ref context, out var stmt))
                return new QsParseResult<QsScriptElement>(new QsStmtScriptElement(stmt!), context);

            return QsParseResult<QsScriptElement>.Invalid;
        }

        public async ValueTask<QsParseResult<QsScript>> ParseScriptAsync(QsParserContext context)
        {
            var elems = ImmutableArray.CreateBuilder<QsScriptElement>();

            while (!Accept<QsEndOfFileToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
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