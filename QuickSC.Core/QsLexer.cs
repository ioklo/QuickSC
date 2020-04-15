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

        async ValueTask<QsLexResult> LexStringModeAsync(QsLexerContext context)
        {
            Debug.Assert(context.LexingMode == QsLexingMode.String);
            
            var textResult = await LexStringModeTextAsync(context);
            if (textResult.HasValue)
                return textResult;

            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    new QsEndStringToken(),
                    context.PopMode(await context.Pos.NextAsync()));

            if (context.Pos.Equals('$'))
            {
                var nextPos = await context.Pos.NextAsync();                

                if (nextPos.Equals('{'))
                    return new QsLexResult(
                        new QsBeginInnerExpToken(),
                        context.PushMode(QsLexingMode.InnerExp, await nextPos.NextAsync()));

                var idResult = await LexIdentifierAsync(context.UpdatePos(nextPos), false);
                if (idResult.HasValue)
                    return idResult;
            }            

            return QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexNormalModeAsync(QsLexerContext context)
        {
            Debug.Assert(context.LexingMode == QsLexingMode.Normal);

            // ��ŵó��
            var skipResult = await SkipAsync(context.Pos);
            if (skipResult.HasValue)
                context = context.UpdatePos(skipResult.Value);
            
            // �� ó��, 
            // TODO: ��� ���ÿ� NormalMode �ϳ��� ���� �����ΰ� Ȯ���ؾ� ���� ������
            if (context.Pos.IsReachEnd())
                return new QsLexResult(
                    new QsEndOfFileToken(),
                    context.UpdateMode(QsLexingMode.Deploted));

            // ������ ����
            var intResult = await LexIntAsync(context);
            if (intResult.HasValue)
                return new QsLexResult(intResult.Token, intResult.Context);

            var boolResult = await LexBoolAsync(context);
            if (boolResult.HasValue)
                return new QsLexResult(boolResult.Token, boolResult.Context);

            // ������ �ɺ� ó��
            if (context.Pos.Equals(';'))
                return new QsLexResult(new QsSemiColonToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals(','))
                return new QsLexResult(new QsCommaToken(), context.UpdatePos(await context.Pos.NextAsync()));

            if (context.Pos.Equals('='))
                return new QsLexResult(new QsEqualToken(), context.UpdatePos(await context.Pos.NextAsync()));

            // "�̸� ��Ʈ�� ó�� ���� �����ϰ� BeginString ����
            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    new QsBeginStringToken(), 
                    context.PushMode(QsLexingMode.String, await context.Pos.NextAsync()));

            // Identifier �õ�
            var idResult = await LexIdentifierAsync(context, true);
            if (idResult.HasValue)
                return new QsLexResult(idResult.Token, idResult.Context);

            return QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexInnerExpModeAsync(QsLexerContext context)
        {
            Debug.Assert(context.LexingMode == QsLexingMode.InnerExp);

            // ��ŵó��
            var skipResult = await SkipAsync(context.Pos);
            if (skipResult.HasValue)
                context = context.UpdatePos(skipResult.Value);

            // }�� ��ó���� �����ϰ�� Normal���� ����
            if (context.Pos.IsReachEnd())
                return QsLexResult.Invalid; // ��ݾ��� ��
            
            if (context.Pos.Equals('}'))
                return new QsLexResult(
                    new QsEndInnerExpToken(),
                    context.PopMode(await context.Pos.NextAsync()));

            if (context.Pos.Equals('"'))
                return new QsLexResult(
                    new QsBeginStringToken(), 
                    context.PushMode(QsLexingMode.String, await context.Pos.NextAsync()));
            
            // Identifier �õ�
            var idResult = await LexIdentifierAsync(context, true);
            if (idResult.HasValue)
                return new QsLexResult(idResult.Token, idResult.Context);

            return QsLexResult.Invalid;
        }

        async ValueTask<QsLexResult> LexCommandModeAsync(QsLexerContext context)
        {
            Debug.Assert(context.LexingMode == QsLexingMode.Command);

            var nextSkipPos = await SkipAsync(context.Pos, bExceptLineSeparator: true);
            if (nextSkipPos != null)
                return new QsLexResult(new QsWhitespaceToken(), context.UpdatePos(nextSkipPos.Value));

            // �� ����
            if (context.Pos.IsReachEnd())
                return new QsLexResult(new QsEndOfCommandToken(), context.UpdateMode(QsLexingMode.Normal));

            // �ٹٲ�
            if (context.Pos.GetUnicodeCategory() == UnicodeCategory.LineSeparator)
            {
                var nextLineSepPos = await context.Pos.NextAsync();
                return new QsLexResult(new QsEndOfCommandToken(), context.Update(QsLexingMode.Normal, nextLineSepPos));
            }

            // "�̸� ��Ʈ�� ó�� ���� �����ϰ� BeginString ����
            if (context.Pos.Equals('"'))
            {
                var nextQuotePos = await context.Pos.NextAsync();
                if (!nextQuotePos.Equals('"'))
                {
                    return new QsLexResult(
                        new QsBeginStringToken(),
                        context.PushMode(QsLexingMode.String, await context.Pos.NextAsync())); // ������ CommandArgument�� �����ϵ���
                }
            }

            if (context.Pos.Equals('$'))
            {                
                var nextDollarPos = await context.Pos.NextAsync();

                if (nextDollarPos.Equals('{'))
                {
                    return new QsLexResult(
                        new QsBeginInnerExpToken(),
                        context.PushMode(QsLexingMode.InnerExp, await nextDollarPos.NextAsync()));
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
            Debug.Assert(context.LexingMode == QsLexingMode.String);

            var sb = new StringBuilder();
            var curPos = context.Pos;
            while (true) // ����
            {
                if (curPos.IsReachEnd())
                    return QsLexResult.Invalid;

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
        
        async ValueTask<QsBufferPosition?> SkipAsync(QsBufferPosition pos, bool bExceptLineSeparator = false)
        {
            var curPos = pos;

            while(!curPos.IsReachEnd() && curPos.IsWhiteSpace() && (!bExceptLineSeparator || curPos.GetUnicodeCategory() != UnicodeCategory.LineSeparator))
            {
                curPos = await curPos.NextAsync();
            }

            return (curPos == pos) ? (QsBufferPosition?)null : curPos;
        }

        public async ValueTask<QsLexResult> LexAsync(QsLexerContext context)
        {
            // �� �Ǿ����� ���̻� �ƹ��͵� �������� �ʴ´�
            if (context.LexingMode == QsLexingMode.Deploted)
                return QsLexResult.Invalid;

            Debug.Assert(context.LexingMode != QsLexingMode.Deploted);
            switch (context.LexingMode)
            {
                case QsLexingMode.Normal:
                    return await LexNormalModeAsync(context);

                case QsLexingMode.String:
                    return await LexStringModeAsync(context);

                case QsLexingMode.InnerExp:
                    return await LexInnerExpModeAsync(context);

                case QsLexingMode.Command:
                    return await LexCommandModeAsync(context);
            }
            
            return QsLexResult.Invalid;
        }
    }
}