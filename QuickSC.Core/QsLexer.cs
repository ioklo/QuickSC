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
                    new QsDoubleQuoteToken(),
                    context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('$'))
            {
                var nextPos = await context.Pos.NextAsync();                

                if (nextPos.Equals('{'))
                    return new QsLexResult(
                        new QsDollarLBraceToken(),
                        context.UpdatePos(await nextPos.NextAsync()));

                var idResult = await LexIdentifierAsync(context.UpdatePos(nextPos), false);
                if (idResult.HasValue)
                    return idResult;
            }            

            return QsLexResult.Invalid;
        }

        public async ValueTask<QsLexResult> LexNormalModeAsync(QsLexerContext context, bool bSkipWhitespaceAndNewLine = true)
        {
            // ��ŵó��
            if (bSkipWhitespaceAndNewLine)
            {
                while (true)
                {
                    var wsResult = await LexWhitespaceAsync(context, bIncludeNewLine: true);
                    if (wsResult.HasValue)
                    {
                        context = wsResult.Context;
                        continue;
                    }

                    break;
                }
            }

            // �� ó��, 
            // TODO: ��� ���ÿ� NormalMode �ϳ��� ���� �����ΰ� Ȯ���ؾ� ���� ������
            if (context.Pos.IsReachEnd())
                return new QsLexResult(
                    new QsEndOfFileToken(),
                    context);

            // ������ ����
            var intResult = await LexIntAsync(context);
            if (intResult.HasValue)
                return new QsLexResult(intResult.Token, intResult.Context);

            var boolResult = await LexBoolAsync(context);
            if (boolResult.HasValue)
                return new QsLexResult(boolResult.Token, boolResult.Context);

            // Ű���� ó��
            var ifResult = await ConsumeAsync("if", context.Pos);
            if (ifResult.HasValue)
                return new QsLexResult(new QsIfToken(), context.UpdatePos(ifResult.Value));

            var elseResult = await ConsumeAsync("else", context.Pos);
            if (elseResult.HasValue)
                return new QsLexResult(new QsElseToken(), context.UpdatePos(elseResult.Value));

            var forResult = await ConsumeAsync("for", context.Pos);
            if (forResult.HasValue)
                return new QsLexResult(new QsForToken(), context.UpdatePos(forResult.Value));

            var continueResult = await ConsumeAsync("continue", context.Pos);
            if (continueResult.HasValue)
                return new QsLexResult(new QsContinueToken(), context.UpdatePos(continueResult.Value));

            var breakResult = await ConsumeAsync("break", context.Pos);
            if (breakResult.HasValue)
                return new QsLexResult(new QsBreakToken(), context.UpdatePos(breakResult.Value));

            var plusplusResult = await ConsumeAsync("++", context.Pos);
            if (plusplusResult.HasValue)
                return new QsLexResult(new QsPlusPlusToken(), context.UpdatePos(plusplusResult.Value));

            var minusminusResult = await ConsumeAsync("--", context.Pos);
            if (minusminusResult.HasValue)
                return new QsLexResult(new QsMinusMinusToken(), context.UpdatePos(minusminusResult.Value));

            var lessthanequalResult = await ConsumeAsync("<=", context.Pos);
            if (lessthanequalResult.HasValue)
                return new QsLexResult(new QsLessThanEqualToken(), context.UpdatePos(lessthanequalResult.Value));

            var greaterthanequalResult = await ConsumeAsync(">=", context.Pos);
            if (greaterthanequalResult.HasValue)
                return new QsLexResult(new QsGreaterThanEqualToken(), context.UpdatePos(greaterthanequalResult.Value));

            // ������ �ɺ� ó��
            if (context.Pos.Equals('<'))
                return new QsLexResult(new QsLessThanToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('>'))
                return new QsLexResult(new QsGreaterThanToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals(';'))
                return new QsLexResult(new QsSemiColonToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals(','))
                return new QsLexResult(new QsCommaToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('='))
                return new QsLexResult(new QsEqualToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('{'))
                return new QsLexResult(new QsLBraceToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('}'))
                return new QsLexResult(new QsRBraceToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('('))
                return new QsLexResult(new QsLParenToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals(')'))
                return new QsLexResult(new QsRParenToken(), context.UpdatePos(await context.Pos.NextAsync()));


            // "�̸� ��Ʈ�� ó�� ���� �����ϰ� BeginString ����
            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    new QsDoubleQuoteToken(), 
                    context.UpdatePos(await context.Pos.NextAsync()));

            // Identifier �õ�
            var idResult = await LexIdentifierAsync(context, true);
            if (idResult.HasValue)
                return new QsLexResult(idResult.Token, idResult.Context);

            return QsLexResult.Invalid;
        }
        
        public async ValueTask<QsLexResult> LexCommandModeAsync(QsLexerContext context)
        {
            var wsResult = await LexWhitespaceAsync(context, bIncludeNewLine: false);
            if (wsResult.HasValue)
                return new QsLexResult(new QsWhitespaceToken(), wsResult.Context);

            var newLineResult = await LexNewLineAsync(context);
            if (newLineResult.HasValue)
                return new QsLexResult(new QsEndOfCommandToken(), newLineResult.Context);

            // �� ����
            if (context.Pos.IsReachEnd())
                return new QsLexResult(new QsEndOfCommandToken(), context);
            
            // "�̸� ��Ʈ�� ó�� ���� �����ϰ� BeginString ����
            if (context.Pos.Equals('"'))
            {
                var nextQuotePos = await context.Pos.NextAsync();
                if (!nextQuotePos.Equals('"'))
                {
                    return new QsLexResult(
                        new QsDoubleQuoteToken(),
                        context.UpdatePos(await context.Pos.NextAsync())); // ������ CommandArgument�� �����ϵ���
                }
            }

            if (context.Pos.Equals('$'))
            {                
                var nextDollarPos = await context.Pos.NextAsync();

                if (nextDollarPos.Equals('{'))
                {
                    return new QsLexResult(
                        new QsDollarLBraceToken(),
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

            // �������� text���
            while(true)
            {
                // �� ����
                if (context.Pos.IsReachEnd()) break;
                
                // Whitespace, �ٹٲ�
                if (context.Pos.IsWhiteSpace()) break;

                if (context.Pos.Equals('"'))
                {
                    var nextQuotePos = await context.Pos.NextAsync();
                    if (nextQuotePos.Equals('"'))
                    {
                        sb.Append('"');
                        context = context.UpdatePos(await nextQuotePos.NextAsync());
                        continue;
                    }

                    break;
                }

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

        async ValueTask<QsLexResult> LexIntAsync(QsLexerContext context)
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
            while (true) // ����
            {
                if (curPos.IsReachEnd())
                    break;

                if (curPos.Equals('"')) // "�ΰ� ó��
                {
                    var secondPos = await curPos.NextAsync();
                    if (!secondPos.Equals('"')) break;

                    sb.Append('"');
                    curPos = await secondPos.NextAsync();
                }
                else if (curPos.Equals('$')) // $ ó��
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
        
        async ValueTask<QsLexResult> LexWhitespaceAsync(QsLexerContext context, bool bIncludeNewLine)
        {
            bool bUpdated = false;

            while(true)
            {
                if (context.Pos.IsReachEnd()) break;
                if (!context.Pos.IsWhiteSpace()) break;

                // whitespace�ε� lineseparator��� ����
                if (!bIncludeNewLine && (context.Pos.Equals('\r') || context.Pos.Equals('\n'))) break;

                context = context.UpdatePos(await context.Pos.NextAsync());
                bUpdated = true;
            }

            return bUpdated ? new QsLexResult(new QsWhitespaceToken(), context) : QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexNewLineAsync(QsLexerContext context)
        {
            bool bUpdated = false;
            while (context.Pos.Equals('\r') || context.Pos.Equals('\n'))
            {
                context = context.UpdatePos(await context.Pos.NextAsync());
                bUpdated = true;
            }

            return bUpdated ? new QsLexResult(new QsNewLineToken(), context) : QsLexResult.Invalid;
        }

        //public async ValueTask<QsLexResult> LexAsync(QsLexerContext context, bool bSkipWhitespaceAndNewLine)
        //{   
        //    switch (context.LexingMode)
        //    {
        //        case QsLexingMode.Normal:
        //            return await LexNormalModeAsync(context);

        //        case QsLexingMode.String:
        //            return await LexStringModeAsync(context);

        //        case QsLexingMode.InnerExp:
        //            return await LexInnerExpModeAsync(context);

        //        case QsLexingMode.Command:
        //            return await LexCommandModeAsync(context);

        //        // �� �Ǿ����� ���̻� �ƹ��͵� �������� �ʴ´�
        //        case QsLexingMode.Deploted:
        //            return QsLexResult.Invalid;
        //        }

        //    return QsLexResult.Invalid;
        //}
    }
}