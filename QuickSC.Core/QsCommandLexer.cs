using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using QuickSC.Syntax;
using QuickSC.Token;

namespace QuickSC
{
    class QsCommandLexer
    {   
        QsNormalLexer normalLexer;

        public QsCommandLexer(QsNormalLexer normalLexer)
        {
            this.normalLexer = normalLexer;
        }

        // LineSeparator를 뺀다
        async ValueTask<QsBufferPosition?> SkipAsync(QsBufferPosition pos)
        {
            QsBufferPosition curPos = pos;
            
            while (!curPos.IsReachEnd() && curPos.IsWhiteSpace() && curPos.GetUnicodeCategory() != UnicodeCategory.LineSeparator)
            {
                curPos = await curPos.NextAsync();
            }

            return (curPos != pos) ? (QsBufferPosition?)null : curPos;
        }

        public async ValueTask<(QsCommandToken Token, QsBufferPosition NextPos)?> GetNextCommandTokenAsync(QsBufferPosition pos)
        {
            QsBufferPosition curPos = pos;            
            
            var nextPos = await SkipAsync(curPos);
            if (nextPos != null)
                curPos = nextPos.Value;

            // " 으로 시작하는거 먼저 확인
            var stringResult = await normalLexer.LexStringAsync(curPos);
            if (stringResult.HasValue)
                return (new QsStringCommandToken(stringResult.Value.Token), stringResult.Value.NextPos);

            // id로 되는지 검사 (로컬 함수는 identifier만 허용한다)
            var idResult = await normalLexer.LexIdentifierAsync(curPos);
            if (idResult.HasValue)
                return (new QsIdentifierCommandToken(idResult.Value.Token), idResult.Value.NextPos);

            // id가 아닌, $를 포함하는 텍스트
            var bareStringResult = await LexBareStringAsync(curPos);
            if (bareStringResult.HasValue)
                return (new QsStringCommandToken(bareStringResult.Value.Token), bareStringResult.Value.NextPos);

            return null;
        }

        public async ValueTask<(QsCommandArgToken Token, QsBufferPosition NextPos)?> GetNextArgTokenAsync(QsBufferPosition pos)
        {
            QsBufferPosition curPos = pos;
            
            var nextPos = await SkipAsync(curPos);
            if (nextPos != null)
                curPos = nextPos.Value;

            // 1. 스킵하고 보니 끝에 도달했다
            if (curPos.IsReachEnd())
                return (new QsEndOfCommandArgToken(), curPos);

            // 2. 스킵하고 보니 줄바꿈이다
            if (curPos.GetUnicodeCategory() == UnicodeCategory.LineSeparator)
            {
                var nextLineSepPos = await curPos.NextAsync();
                return (new QsEndOfCommandArgToken(), nextLineSepPos);
            }

            // " 으로 시작하는거 먼저 확인
            var stringResult = await normalLexer.LexStringAsync(curPos);
            if (stringResult.HasValue)
                return (new QsStringCommandArgToken(stringResult.Value.Token), stringResult.Value.NextPos);

            // id가 아닌, $를 포함하는 텍스트
            var bareStringResult = await LexBareStringAsync(curPos);
            if (bareStringResult.HasValue)
                return (new QsStringCommandArgToken(bareStringResult.Value.Token), bareStringResult.Value.NextPos);

            return null;
        }

        async ValueTask<(QsStringToken Token, QsBufferPosition NextPos)?> LexBareStringAsync(QsBufferPosition pos)
        {
            QsBufferPosition curPos = pos;

            if (curPos.IsReachEnd()) return null;

            var elems = new List<QsStringTokenElement>();
            var sb = new StringBuilder();
            while (!curPos.IsReachEnd())
            {
                // 띄어쓰기를 만나면 중단
                if (curPos.IsWhiteSpace()) break;

                // 첫문자가 DOLLAR 일때
                if (curPos.Equals('$'))
                {
                    var secondPos = await curPos.NextAsync();

                    // 두번째 문자도 $라면 $
                    if (!secondPos.IsReachEnd() && secondPos.Equals('$')) // TODO: IsReachEnd 제거하기
                    {
                        sb.Append('$');
                        curPos = await secondPos.NextAsync();
                        continue;
                    }

                    // id인지
                    var idResult = await normalLexer.LexIdentifierAsync(secondPos);
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
                    if (!secondPos.IsReachEnd() && secondPos.Equals('{')) // TODO: IsReachEnd 제거하기
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
                            var tokenResult = await normalLexer.GetNextTokenAsync(curPos);

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
    }
}