using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    class QsNormalLexer
    {
        public QsNormalLexer()
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
            // TODO: 다시 한번 쭉 훑어봐야 한다
            return pos.IsWhiteSpace() || pos.Equals('{') || pos.Equals('}');
        }

        public async ValueTask<(QsIdentifierToken Token, QsBufferPosition NextPos)?> LexIdentifierAsync(QsBufferPosition pos)
        {
            if (pos.IsReachEnd()) return null;

            var sb = new StringBuilder();
            QsBufferPosition curPos = pos;

            if (curPos.Equals('@'))
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
                return null;
            }

            while (!curPos.IsReachEnd())
            {
                if (!IsIdentifierLetter(curPos))
                {
                    if (!IsAllowedAfterIdentifierLetter(curPos)) return null;
                    break;
                }

                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            return (new QsIdentifierToken(sb.ToString()), curPos);
        }

        public async ValueTask<(QsStringToken Token, QsBufferPosition NextPos)?> LexStringAsync(QsBufferPosition pos)
        {
            QsBufferPosition curPos = pos;

            if (curPos.IsReachEnd()) return null;

            // starts with quote
            if (!curPos.Equals('"')) return null;
            curPos = await curPos.NextAsync();

            var elems = new List<QsStringTokenElement>();
            var sb = new StringBuilder();
            while (!curPos.IsReachEnd())
            {
                // "" 처리 
                var secondPos = await curPos.NextAsync();
                if (!secondPos.IsReachEnd() && curPos.Equals('"') && secondPos.Equals('"')) // TODO: IsReachEnd 제거
                {
                    sb.Append('"');
                    curPos = await secondPos.NextAsync();
                    continue;
                }

                if (curPos.Equals('"'))
                {
                    curPos = await curPos.NextAsync();
                    break;
                }

                // 첫문자가 DOLLAR 일때
                if (curPos.Equals('$'))
                {
                    // 두번째 문자도 $라면 $
                    if (!secondPos.IsReachEnd() && secondPos.Equals('$')) // TODO: IsReachEnd 제거
                    {
                        sb.Append('$');
                        curPos = await secondPos.NextAsync();
                        continue;
                    }

                    // id인지
                    var idResult = await LexIdentifierAsync(secondPos);
                    if (idResult != null)
                    {
                        // sb 닫고
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        elems.Add(new QsTokenStringTokenElement(idResult.Value.Token));
                        curPos = idResult.Value.NextPos;
                        continue;
                    }

                    // { 라면, } 나올때까지;
                    if (!secondPos.IsReachEnd() && secondPos.Equals('{')) // TODO: IsReachEnd 제거
                    {
                        curPos = await secondPos.NextAsync();

                        // sb 닫고
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (!curPos.IsReachEnd() && !curPos.Equals('}'))
                        {
                            var tokenResult = await GetNextTokenAsync(curPos);

                            // 토큰 얻기에 실패했다면 invalid
                            if (!tokenResult.HasValue)
                                throw new InvalidOperationException();

                            elems.Add(new QsTokenStringTokenElement(tokenResult.Value.Token));
                            curPos = tokenResult.Value.NextPos;
                        }

                        curPos = await curPos.NextAsync();
                        continue;
                    }
                }

                curPos.AppendTo(sb);
                curPos = await curPos.NextAsync();
            }

            if (sb.Length != 0)
            {
                elems.Add(new QsTextStringTokenElement(sb.ToString()));
                sb.Clear();
            }

            if (elems.Count != 0)
                return (new QsStringToken(elems), curPos);

            return null;
        }
        
        async ValueTask<QsBufferPosition?> SkipAsync(QsBufferPosition pos)
        {
            var curPos = pos;

            while(curPos.IsReachEnd() && curPos.IsWhiteSpace())
            {
                curPos = await curPos.NextAsync();
            }

            return (curPos == pos) ? (QsBufferPosition?)null : curPos;
        }
        
        public async ValueTask<(QsToken Token, QsBufferPosition NextPos)?> GetNextTokenAsync(QsBufferPosition pos)
        {
            var curPos = pos;
            while (!curPos.IsReachEnd())
            {
                var nextPos = await SkipAsync(curPos);
                if (nextPos != null)
                {
                    curPos = nextPos.Value;
                    continue;
                }

                var idResult = await LexIdentifierAsync(curPos);
                if (idResult.HasValue)
                    return idResult;

                var stringResult = await LexStringAsync(curPos);
                if (stringResult.HasValue)
                    return stringResult;
            }

            if (curPos.IsReachEnd())
                return (new QsEndOfFileToken(), curPos);

            return null;
        }
    }
}