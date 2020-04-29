using QuickSC.Syntax;
using QuickSC.Token;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace QuickSC
{
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

        async ValueTask<QsParseResult<QsTypeExp>> ParseTypeIdExpAsync(QsParserContext context)
        {
            var idTokenResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (idTokenResult == null)
                return QsParseResult<QsTypeExp>.Invalid;

            return new QsParseResult<QsTypeExp>(new QsTypeIdExp(idTokenResult.Value), context);
        }

        async ValueTask<QsParseResult<QsTypeExp>> ParseTypeExpAsync(QsParserContext context)
        {
            // TODO: 일단 TypeId만
            var typeIdExpResult = await ParseTypeIdExpAsync(context);
            if (typeIdExpResult.HasValue)
                return new QsParseResult<QsTypeExp>(typeIdExpResult.Elem, typeIdExpResult.Context);

            return QsParseResult<QsTypeExp>.Invalid;
        }

        // int a, 
        async ValueTask<QsParseResult<(QsFuncDeclParam FuncDeclParam, bool bVariadic)>> ParseFuncDeclParamAsync(QsParserContext context)
        {
            var bVariadic = Accept<QsParamsToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);

            var typeExpResult = await ParseTypeExpAsync(context);
            if (!typeExpResult.HasValue)
                return QsParseResult<(QsFuncDeclParam, bool)>.Invalid;

            context = typeExpResult.Context;

            var nameResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (nameResult == null)
                return QsParseResult<(QsFuncDeclParam, bool)>.Invalid;

            return new QsParseResult<(QsFuncDeclParam, bool)>((new QsFuncDeclParam(typeExpResult.Elem, nameResult.Value), bVariadic), context);
        }

        internal async ValueTask<QsParseResult<QsFuncDecl>> ParseFuncDeclAsync(QsParserContext context)
        {
            // <Async> <RetTypeName> <FuncName> <LPAREN> <ARGS> <RPAREN>
            // LBRACE>
            // [Stmt]
            // <RBRACE>
            QsFuncKind funcKind;
            if (Accept<QsAsyncToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                funcKind = QsFuncKind.Async;
            else
                funcKind = QsFuncKind.Sync;

            var retTypeResult = await ParseTypeExpAsync(context);
            if (!retTypeResult.HasValue)
                return Invalid();
            context = retTypeResult.Context;

            var funcNameResult = AcceptAndReturn<QsIdentifierToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context);
            if (funcNameResult == null)
                return Invalid();

            if (!Accept<QsLParenToken>(await lexer.LexNormalModeAsync(context.LexerContext, true), ref context))
                return Invalid();

            var funcDeclParams = ImmutableArray.CreateBuilder<QsFuncDeclParam>();
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

            return new QsParseResult<QsFuncDecl>(new QsFuncDecl(funcKind, retTypeResult.Elem, funcNameResult.Value, funcDeclParams.ToImmutable(), variadicParamIndex, blockStmtResult.Elem), context);

            static QsParseResult<QsFuncDecl> Invalid() => QsParseResult<QsFuncDecl>.Invalid;
        }

        async ValueTask<QsParseResult<QsScriptElement>> ParseScriptElementAsync(QsParserContext context)
        {
            var funcDeclResult = await ParseFuncDeclAsync(context);
            if (funcDeclResult.HasValue)
                return new QsParseResult<QsScriptElement>(new QsFuncDeclScriptElement(funcDeclResult.Elem), funcDeclResult.Context);

            var stmtResult = await stmtParser.ParseStmtAsync(context);
            if (stmtResult.HasValue) 
                return new QsParseResult<QsScriptElement>(new QsStmtScriptElement(stmtResult.Elem), stmtResult.Context);

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