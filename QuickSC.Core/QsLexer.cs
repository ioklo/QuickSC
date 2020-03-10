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
            // id로 되는지 검사 (로컬 함수는 identifier만 허용한다)
            var idResult = LexIdentifier(text, pos);
            if (idResult.HasValue)
                return idResult;

            // id가 아닌, $를 포함하는 텍스트
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
            // TODO: 다시 한번 쭉 훑어봐야 한다
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
                // 띄어쓰기를 만나면 중단
                if (char.IsWhiteSpace(text, curPos)) break;

                // 첫문자가 DOLLAR 일때
                if (text[curPos] == '$')
                {
                    // 두번째 문자도 $라면 $
                    if (curPos + 1 < text.Length && text[curPos + 1] == '$')
                    {
                        sb.Append('$');
                        curPos += 2;
                        continue;
                    }

                    // id인지
                    var idResult = LexIdentifier(text, curPos + 1);
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
                    if (curPos + 1 < text.Length && text[curPos + 1] == '{')
                    {
                        curPos += 2;

                        // sb 닫고
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (curPos < text.Length && text[curPos] != '}')
                        {
                            var tokenResult = LexToken(text, curPos);

                            // 토큰 얻기에 실패했다면 invalid
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
                // "" 처리 
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

                // 첫문자가 DOLLAR 일때
                if (text[curPos] == '$')
                {
                    // 두번째 문자도 $라면 $
                    if (curPos + 1 < text.Length && text[curPos + 1] == '$')
                    {
                        sb.Append('$');
                        curPos += 2;
                        continue;
                    }

                    // id인지
                    var idResult = LexIdentifier(text, curPos + 1);
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
                    if (curPos + 1 < text.Length && text[curPos + 1] == '{')
                    {
                        curPos += 2;

                        // sb 닫고
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (curPos < text.Length && text[curPos] != '}')
                        {
                            var tokenResult = LexToken(text, curPos);

                            // 토큰 얻기에 실패했다면 invalid
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