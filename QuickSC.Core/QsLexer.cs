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
    class QsLexer
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

        bool IsAllowedAfterIdentifierLetter(QsBufferPosition pos)
        {
            // TODO: �ٽ� �ѹ� �� �Ⱦ���� �Ѵ�
            return pos.IsWhiteSpace() || pos.Equals('{') || pos.Equals('}');
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
                        new QsBeginStringExpToken(),
                        context.PushMode(QsLexingMode.StringExp, await nextPos.NextAsync()));

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

            // ��ŵ�ϸ� ���� �ٽ� ó�� �����
            if (context.Pos.IsReachEnd())
                return new QsLexResult(
                    new QsEndOfFileToken(),
                    context.UpdateMode(QsLexingMode.Deploted));

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

        async ValueTask<QsLexResult> LexStringExpModeAsync(QsLexerContext context)
        {
            Debug.Assert(context.LexingMode == QsLexingMode.StringExp);

            // ��ŵó��
            var skipResult = await SkipAsync(context.Pos);
            if (skipResult.HasValue)
                context = context.UpdatePos(skipResult.Value);

            // ��ŵ�ϸ� ���� �ٽ� ó�� �����
            if (context.Pos.IsReachEnd())
                return new QsLexResult(
                    new QsEndOfFileToken(),
                    context.UpdateMode(QsLexingMode.Deploted));

            // �̰� �����ϰ�� Normal���� ����
            if (context.Pos.Equals('}'))
                return new QsLexResult(
                    new QsEndStringExpToken(),
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
                return new QsLexResult(new QsEndOfCommandTokenToken(), context.UpdateMode(QsLexingMode.Normal));

            // �ٹٲ�
            if (context.Pos.GetUnicodeCategory() == UnicodeCategory.LineSeparator)
            {
                var nextLineSepPos = await context.Pos.NextAsync();
                return new QsLexResult(new QsEndOfCommandTokenToken(), context.Update(QsLexingMode.Normal, nextLineSepPos));
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
                        new QsBeginStringExpToken(),
                        context.PushMode(QsLexingMode.StringExp, await nextDollarPos.NextAsync()));
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
            if (context.Pos.IsReachEnd())
                return QsLexResult.Invalid;

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

            while (!curPos.IsReachEnd())
            {
                if (!IsIdentifierLetter(curPos))
                {
                    if (!IsAllowedAfterIdentifierLetter(curPos))
                        return QsLexResult.Invalid;

                    break;
                }

                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            if (sb.Length == 0)
                return QsLexResult.Invalid;

            return new QsLexResult(new QsIdentifierToken(sb.ToString()), context.UpdatePos(curPos));
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

            // ������ ���� ó������ �ʾ����� 
            if (context.Pos.IsReachEnd())
                return new QsLexResult(
                    new QsEndOfFileToken(), 
                    context.UpdateMode(QsLexingMode.Deploted));

            Debug.Assert(context.LexingMode != QsLexingMode.Deploted);
            switch (context.LexingMode)
            {
                case QsLexingMode.Normal:
                    return await LexNormalModeAsync(context);

                case QsLexingMode.String:
                    return await LexStringModeAsync(context);

                case QsLexingMode.StringExp:
                    return await LexStringExpModeAsync(context);

                case QsLexingMode.Command:
                    return await LexCommandModeAsync(context);
            }
            
            return QsLexResult.Invalid;
        }
    }
}