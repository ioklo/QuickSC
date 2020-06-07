using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    public struct QsLexResult
    {
        public static QsLexResult Invalid { get; }
        static QsLexResult()
        {
            Invalid = new QsLexResult();
        }

        public bool HasValue { get; }
        public QsToken Token { get; }
        public QsLexerContext Context { get; }
        public QsLexResult(QsToken token, QsLexerContext context) { HasValue = true; Token = token; Context = context; }
    }

    public class QsLexer
    {
        public QsLexer()
        {
        }

        bool IsIdentifierStartLetter(QsBufferPosition curPos)
        {
            if (curPos.Equals('_')) return true; // only allowed among ConnectorPunctuation category

            var category = curPos.GetUnicodeCategory();

            return category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.DecimalDigitNumber;
        }

        bool IsIdentifierLetter(QsBufferPosition curPos)
        {
            if (curPos.Equals('_')) return true; // only allowed among ConnectorPunctuation category

            var category = curPos.GetUnicodeCategory();

            return category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.DecimalDigitNumber;
        }

        public async ValueTask<QsLexResult> LexStringModeAsync(QsLexerContext context)
        {   
            var textResult = await LexStringModeTextAsync(context);
            if (textResult.HasValue)
                return textResult;

            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    QsDoubleQuoteToken.Instance,
                    context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('$'))
            {
                var nextPos = await context.Pos.NextAsync();                

                if (nextPos.Equals('{'))
                    return new QsLexResult(
                        QsDollarLBraceToken.Instance,
                        context.UpdatePos(await nextPos.NextAsync()));

                var idResult = await LexIdentifierAsync(context.UpdatePos(nextPos), false);
                if (idResult.HasValue)
                    return idResult;
            }            

            return QsLexResult.Invalid;
        }

        // 키워드 처리
        private static Dictionary<string, QsToken> keywordInfos = new Dictionary<string, QsToken>()
        {
            { "foreach", QsForeachToken.Instance },
            { "if", QsIfToken.Instance },
            { "else", QsElseToken.Instance },
            { "for", QsForToken.Instance },
            { "continue", QsContinueToken.Instance },
            { "break", QsBreakToken.Instance },
            { "exec", QsExecToken.Instance },
            { "task", QsTaskToken.Instance },
            { "params", QsParamsToken.Instance },
            { "return", QsReturnToken.Instance },
            { "async", QsAsyncToken.Instance },
            { "await", QsAwaitToken.Instance },
            { "in", QsInToken.Instance },
            { "yield", QsYieldToken.Instance },
            { "seq", QsSeqToken.Instance },
            { "enum", QsEnumToken.Instance },
            { "is", QsIsToken.Instance },

        };

        private static (string Text, Func<QsToken> Constructor)[] infos = new (string Text, Func<QsToken> Constructor)[]
        {   
            ("++", () => QsPlusPlusToken.Instance),
            ("--", () => QsMinusMinusToken.Instance),
            ("<=", () => QsLessThanEqualToken.Instance),
            (">=", () => QsGreaterThanEqualToken.Instance),
            ("=>", () => QsEqualGreaterThanToken.Instance),
            ("==", () => QsEqualEqualToken.Instance),
            ("!=", () => QsExclEqualToken.Instance),


            ("@", () => QsExecToken.Instance),
            ("<", () => QsLessThanToken.Instance),
            (">", () => QsGreaterThanToken.Instance),
            (";", () => QsSemiColonToken.Instance),
            (",", () => QsCommaToken.Instance),
            ("=", () => QsEqualToken.Instance),
            ("{", () => QsLBraceToken.Instance),
            ("}", () => QsRBraceToken.Instance),
            ("(", () => QsLParenToken.Instance),
            (")", () => QsRParenToken.Instance),
            ("[", () => QsLBracketToken.Instance),
            ("]", () => QsRBracketToken.Instance),


            ("+", () => QsPlusToken.Instance),
            ("-", () => QsMinusToken.Instance),
            ("*", () => QsStarToken.Instance),
            ("/", () => QsSlashToken.Instance),
            ("%", () => QsPercentToken.Instance),
            ("!", () => QsExclToken.Instance),
            (".", () => QsDotToken.Instance),
        };

        public async ValueTask<QsLexResult> LexNormalModeAsync(QsLexerContext context, bool bSkipNewLine)
        {
            // 스킵처리
            var wsResult = await LexWhitespaceAsync(context, bSkipNewLine);
            if (wsResult.HasValue)
                context = wsResult.Context;

            // 끝 처리
            if (context.Pos.IsReachEnd())
                return new QsLexResult(QsEndOfFileToken.Instance, context);

            // 줄바꿈 문자
            var newLineResult = await LexNewLineAsync(context);
            if(newLineResult.HasValue)
                return new QsLexResult(newLineResult.Token, newLineResult.Context);

            // 여러개 먼저
            var intResult = await LexIntAsync(context);
            if (intResult.HasValue)
                return new QsLexResult(intResult.Token, intResult.Context);

            var boolResult = await LexBoolAsync(context);
            if (boolResult.HasValue)
                return new QsLexResult(boolResult.Token, boolResult.Context);

            foreach (var info in infos)
            {
                var consumeResult = await ConsumeAsync(info.Text, context.Pos);
                if (consumeResult.HasValue)
                    return new QsLexResult(info.Constructor(), context.UpdatePos(consumeResult.Value));
            }

            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    QsDoubleQuoteToken.Instance, 
                    context.UpdatePos(await context.Pos.NextAsync()));

            var keywordResult = await LexKeywordAsync(context);
            if (keywordResult.HasValue)
                return new QsLexResult(keywordResult.Token, keywordResult.Context);


            // Identifier 시도
            var idResult = await LexIdentifierAsync(context, true);
            if (idResult.HasValue)
                return new QsLexResult(idResult.Token, idResult.Context);

            return QsLexResult.Invalid;
        }

        public async ValueTask<QsLexResult> LexCommandModeAsync(QsLexerContext context)
        {   
            var newLineResult = await LexNewLineAsync(context);
            if (newLineResult.HasValue)
                return new QsLexResult(QsNewLineToken.Instance, newLineResult.Context);

            // TODO: \} 처리
            if (context.Pos.Equals('}'))
                return new QsLexResult(QsRBraceToken.Instance, context.UpdatePos(await context.Pos.NextAsync()));
            
            if (context.Pos.Equals('$'))
            {                
                var nextDollarPos = await context.Pos.NextAsync();

                if (nextDollarPos.Equals('{'))
                {
                    return new QsLexResult(
                        QsDollarLBraceToken.Instance,
                        context.UpdatePos(await nextDollarPos.NextAsync()));
                }

                if (!nextDollarPos.Equals('$'))
                {
                    var idResult = await LexIdentifierAsync(context.UpdatePos(nextDollarPos), false);
                    if (idResult.HasValue)
                        return idResult;
                }
            }

            var sb = new StringBuilder();

            // 나머지는 text모드
            while(true)
            {
                // 끝 도달
                if (context.Pos.IsReachEnd()) break;
                
                // NewLine문자
                if (context.Pos.Equals('\r') || context.Pos.Equals('\n')) break;

                // TODO: \} 처리
                if (context.Pos.Equals('}'))
                    break;
                
                if (context.Pos.Equals('$'))
                {
                    var nextDollarPos = await context.Pos.NextAsync();
                    if (nextDollarPos.Equals('$'))
                    {
                        sb.Append('$');
                        context = context.UpdatePos(await nextDollarPos.NextAsync());
                        continue;
                    }

                    break;
                }

                context.Pos.AppendTo(sb);
                context = context.UpdatePos(await context.Pos.NextAsync());
            }

            if (0 < sb.Length)
                return new QsLexResult(new QsTextToken(sb.ToString()), context);

            return QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexKeywordAsync(QsLexerContext context)
        {
            var sb = new StringBuilder();
            QsBufferPosition curPos = context.Pos;

            while (IsIdentifierLetter(curPos))
            {
                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            if (sb.Length == 0)
                return QsLexResult.Invalid;

            if (keywordInfos.TryGetValue(sb.ToString(), out var token))
                return new QsLexResult(token, context.UpdatePos(curPos));            

            return QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexIdentifierAsync(QsLexerContext context, bool bAllowRawMark)
        {
            var sb = new StringBuilder();
            QsBufferPosition curPos = context.Pos;

            if (bAllowRawMark && curPos.Equals('@'))
            {
                curPos = await curPos.NextAsync();
            }
            else if (IsIdentifierStartLetter(curPos))
            {
                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }
            else
            {
                return QsLexResult.Invalid;
            }

            while (IsIdentifierLetter(curPos))
            {   
                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            if (sb.Length == 0)
                return QsLexResult.Invalid;

            return new QsLexResult(new QsIdentifierToken(sb.ToString()), context.UpdatePos(curPos));
        }

        async ValueTask<QsBufferPosition?> ConsumeAsync(string text, QsBufferPosition pos)
        {
            foreach (var c in text)
            {
                if (!pos.Equals(c))
                    return null;

                pos = await pos.NextAsync();
            }

            return pos;
        }

        async ValueTask<QsLexResult> LexBoolAsync(QsLexerContext context)
        {
            var trueResult = await ConsumeAsync("true", context.Pos);
            if (trueResult.HasValue)
                return new QsLexResult(new QsBoolToken(true), context.UpdatePos(trueResult.Value));

            var falseResult = await ConsumeAsync("false", context.Pos);
            if (falseResult.HasValue)
                return new QsLexResult(new QsBoolToken(false), context.UpdatePos(falseResult.Value));

            return QsLexResult.Invalid;
        }

        internal async ValueTask<QsLexResult> LexIntAsync(QsLexerContext context)
        {
            var sb = new StringBuilder();
            QsBufferPosition curPos = context.Pos;

            while (curPos.GetUnicodeCategory() == UnicodeCategory.DecimalDigitNumber)
            {   
                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            if (sb.Length == 0)
                return QsLexResult.Invalid;

            return new QsLexResult(new QsIntToken(int.Parse(sb.ToString())), context.UpdatePos(curPos));
        }

        async ValueTask<QsLexResult> LexStringModeTextAsync(QsLexerContext context)
        {
            var sb = new StringBuilder();
            var curPos = context.Pos;
            while (true) // 조심
            {
                if (curPos.IsReachEnd())
                    break;

                if (curPos.Equals('"')) // "두개 처리
                {
                    var secondPos = await curPos.NextAsync();
                    if (!secondPos.Equals('"')) break;

                    sb.Append('"');
                    curPos = await secondPos.NextAsync();
                }
                else if (curPos.Equals('$')) // $ 처리
                {
                    var secondPos = await curPos.NextAsync();
                    if (!secondPos.Equals('$')) break;
                    
                    sb.Append('$');
                    curPos = await secondPos.NextAsync();
                }
                else
                {
                    curPos.AppendTo(sb);
                    curPos = await curPos.NextAsync();
                }
            }

            if (sb.Length == 0)
                return QsLexResult.Invalid;

            return new QsLexResult(new QsTextToken(sb.ToString()), context.UpdatePos(curPos));
        }
        
        internal async ValueTask<QsLexResult> LexWhitespaceAsync(QsLexerContext context, bool bIncludeNewLine)
        {
            QsLexResult? nextLineModeFailedResult = null;

            bool bUpdated = false;
            while(true)
            {
                if (context.Pos.Equals('\\'))
                {
                    nextLineModeFailedResult = bUpdated ? new QsLexResult(QsWhitespaceToken.Instance, context) : QsLexResult.Invalid;
                    context = context.UpdatePos(await context.Pos.NextAsync());
                    continue;
                }

                // 코멘트 처리
                var commentBeginPos = await ConsumeAsync("//", context.Pos);

                if (!commentBeginPos.HasValue)
                    commentBeginPos = await ConsumeAsync("#", context.Pos);

                if (commentBeginPos.HasValue)
                {
                    context = context.UpdatePos(commentBeginPos.Value);

                    while (!context.Pos.IsReachEnd() && !context.Pos.Equals('\r') && !context.Pos.Equals('\n'))
                    {
                        context = context.UpdatePos(await context.Pos.NextAsync());
                        bUpdated = true;
                    }
                    continue;
                }

                if (context.Pos.IsWhiteSpace())
                {
                    context = context.UpdatePos(await context.Pos.NextAsync());
                    bUpdated = true;
                    continue;
                }

                if (bIncludeNewLine && (context.Pos.Equals('\r') || context.Pos.Equals('\n')))
                {
                    context = context.UpdatePos(await context.Pos.NextAsync());
                    bUpdated = true;
                    continue;
                }

                if (nextLineModeFailedResult.HasValue)
                {
                    var rnPos = await ConsumeAsync("\r\n", context.Pos);
                    if (rnPos.HasValue)
                    {
                        nextLineModeFailedResult = null;
                        context = context.UpdatePos(rnPos.Value);
                        bUpdated = true;
                        continue;
                    }
                    else if (context.Pos.Equals('\r') || context.Pos.Equals('\n'))
                    {
                        nextLineModeFailedResult = null;
                        context = context.UpdatePos(await context.Pos.NextAsync());
                        bUpdated = true;
                        continue;
                    }
                    else
                    {
                        // \ 이전을 리턴한다
                        return nextLineModeFailedResult.Value;
                    }
                }

                break;
            }

            return bUpdated ? new QsLexResult(QsWhitespaceToken.Instance, context) : QsLexResult.Invalid;
        }

        internal async ValueTask<QsLexResult> LexNewLineAsync(QsLexerContext context)
        {
            bool bUpdated = false;
            while (context.Pos.Equals('\r') || context.Pos.Equals('\n'))
            {
                context = context.UpdatePos(await context.Pos.NextAsync());
                bUpdated = true;
            }

            return bUpdated ? new QsLexResult(QsNewLineToken.Instance, context) : QsLexResult.Invalid;
        }
    }
}