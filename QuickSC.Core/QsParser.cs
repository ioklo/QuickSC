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

        public QsParserContext AddType(string typeName)
        {
            return new QsParserContext(LexerContext, types.Add(typeName));
        }

        public bool HasType(string typeName)
        {
            return types.Contains(typeName);
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
        QsStmtParser stmtParser;

        public QsParser(QsLexer lexer)
        {
            this.lexer = lexer;            
            stmtParser = new QsStmtParser(lexer);
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
        
        
        async ValueTask<QsParseResult<QsScriptElement>> ParseScriptElementAsync(QsParserContext context)
        {
            var stmtResult = await stmtParser.ParseStmtAsync(context);
            if (stmtResult.HasValue) 
                return new QsParseResult<QsScriptElement>(new QsStmtScriptElement(stmtResult.Elem), stmtResult.Context);

            return new QsParseResult<QsScriptElement>();
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