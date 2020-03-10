using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    class QsLexer
    {
        string text;

        public QsLexer(string text)
        {           
            this.text = text;
        }

        public (QsToken Token, int NextPos)? NextToken(int pos)
        {
            return LexToken(text, pos);
        }

        int? Skip(string text, int pos)
        {
            int curPos = pos;

            while(curPos < text.Length && char.IsWhiteSpace(text, curPos))
            {   
                curPos += char.IsSurrogatePair(text, curPos) ? 2 : 1;
            }

            return curPos == pos ? (int?)null : curPos;
        }

        bool IsIdentifierStartLetter(string text, int curPos)
        {
            if (text[curPos] == '_') return true; // only allowed among ConnectorPunctuation category

            var category = char.GetUnicodeCategory(text, curPos);

            return category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.DecimalDigitNumber;
        }

        bool IsIdentifierLetter(string text, int curPos)
        {
            if (text[curPos] == '_') return true; // only allowed among ConnectorPunctuation category

            var category = char.GetUnicodeCategory(text, curPos);

            return category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.DecimalDigitNumber;
        }

        public (QsToken Token, int NextPos)? LexCommandToken(int pos)
        {
            // id�� �Ǵ��� �˻� (���� �Լ��� identifier�� ����Ѵ�)
            var idResult = LexIdentifier(text, pos);
            if (idResult.HasValue)
                return idResult;

            // id�� �ƴ�, $�� �����ϴ� �ؽ�Ʈ
            var bareStringResult = LexBareString(text, pos);
            if (bareStringResult.HasValue)
                return bareStringResult;

            var stringResult = LexString(text, pos);
            if (stringResult.HasValue)
                return stringResult;

            return null;
        }

        internal (QsToken Token, int NextPos)? LexToken(string text, int pos)
        {
            int curPos = pos;
            while (curPos < text.Length)
            {
                var nextPos = Skip(text, curPos);
                if (nextPos != null)
                {
                    curPos = nextPos.Value;
                    continue;
                }

                var idResult = LexIdentifier(text, curPos);
                if (idResult.HasValue)
                    return idResult;

                var stringResult = LexString(text, curPos);
                if (stringResult.HasValue)
                    return stringResult;
            }

            if (curPos == text.Length)
                return (new QsEOFToken(), curPos);

            return null;
        }

        bool IsAllowedAfterIdentifierLetter(string text, int pos)
        {
            // TODO: �ٽ� �ѹ� �� �Ⱦ���� �Ѵ�
            return char.IsWhiteSpace(text, pos) || text[pos] == '{' || text[pos] == '}';
        }

        (QsIdentifierToken Token, int NextPos)? LexIdentifier(string text, int pos)
        {
            if (text.Length <= pos) return null;

            int startPos = pos; // treat @
            int curPos = pos;

            if (text[curPos] == '@')
            {
                startPos++;
                curPos++;
            }
            else if (IsIdentifierStartLetter(text, curPos))
            {
                curPos++;
            }
            else return null;
            
            while(curPos < text.Length)
            {
                if (!IsIdentifierLetter(text, curPos))
                {
                    if (!IsAllowedAfterIdentifierLetter(text, curPos)) return null;
                    break;
                }
                curPos += char.IsSurrogatePair(text, curPos) ? 2 : 1;
            }

            return (new QsIdentifierToken(text.Substring(startPos, curPos - startPos)), curPos);
        }

        (QsStringToken Token, int NextPos)? LexBareString(string text, int pos)
        {
            int curPos = pos;

            if (text.Length <= curPos) return null;

            var elems = new List<QsStringTokenElement>();
            var sb = new StringBuilder();
            while (curPos < text.Length)
            {
                // ���⸦ ������ �ߴ�
                if (char.IsWhiteSpace(text, curPos)) break;

                // ù���ڰ� DOLLAR �϶�
                if (text[curPos] == '$')
                {
                    // �ι�° ���ڵ� $��� $
                    if (curPos + 1 < text.Length && text[curPos + 1] == '$')
                    {
                        sb.Append('$');
                        curPos += 2;
                        continue;
                    }

                    // id����
                    var idResult = LexIdentifier(text, curPos + 1);
                    if (idResult != null)
                    {
                        // sb �ݰ�
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        elems.Add(new QsTokenStringTokenElement(idResult.Value.Token));
                        curPos = idResult.Value.NextPos;
                        continue;
                    }

                    // { ���, } ���ö�����;
                    if (curPos + 1 < text.Length && text[curPos + 1] == '{')
                    {
                        curPos += 2;

                        // sb �ݰ�
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (curPos < text.Length && text[curPos] != '}')
                        {
                            var tokenResult = LexToken(text, curPos);

                            // ��ū ��⿡ �����ߴٸ� invalid
                            if (!tokenResult.HasValue)
                                throw new InvalidOperationException();

                            elems.Add(new QsTokenStringTokenElement(tokenResult.Value.Token));
                            curPos = tokenResult.Value.NextPos;
                        }

                        curPos++;
                        continue;
                    }
                }

                sb.Append(text[curPos]);
                curPos++;
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

        (QsStringToken Token, int NextPos)? LexString(string text, int pos)
        {
            int curPos = pos;

            if (text.Length <= curPos) return null;

            // starts with quote
            if (text[curPos] != '"') return null;
            curPos++;

            var elems = new List<QsStringTokenElement>();
            var sb = new StringBuilder();
            while (curPos < text.Length)
            {
                // "" ó�� 
                if (curPos + 1 < text.Length && text[curPos] == '"' && text[curPos + 1] == '"')
                {
                    sb.Append('"');
                    curPos += 2;
                    continue;
                }

                if (text[curPos] == '"')
                {
                    curPos++;
                    break;
                }

                // ù���ڰ� DOLLAR �϶�
                if (text[curPos] == '$')
                {
                    // �ι�° ���ڵ� $��� $
                    if (curPos + 1 < text.Length && text[curPos + 1] == '$')
                    {
                        sb.Append('$');
                        curPos += 2;
                        continue;
                    }

                    // id����
                    var idResult = LexIdentifier(text, curPos + 1);
                    if (idResult != null)
                    {
                        // sb �ݰ�
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        elems.Add(new QsTokenStringTokenElement(idResult.Value.Token));
                        curPos = idResult.Value.NextPos;
                        continue;
                    }

                    // { ���, } ���ö�����;
                    if (curPos + 1 < text.Length && text[curPos + 1] == '{')
                    {
                        curPos += 2;

                        // sb �ݰ�
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (curPos < text.Length && text[curPos] != '}')
                        {
                            var tokenResult = LexToken(text, curPos);

                            // ��ū ��⿡ �����ߴٸ� invalid
                            if (!tokenResult.HasValue) 
                                throw new InvalidOperationException();

                            elems.Add(new QsTokenStringTokenElement(tokenResult.Value.Token));
                            curPos = tokenResult.Value.NextPos;
                        }

                        curPos++;
                        continue;
                    }
                }

                sb.Append(text[curPos]);
                curPos++;
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
    }
}