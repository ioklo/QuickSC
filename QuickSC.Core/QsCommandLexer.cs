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

        // LineSeparator�� ����
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

            // " ���� �����ϴ°� ���� Ȯ��
            var stringResult = await normalLexer.LexStringAsync(curPos);
            if (stringResult.HasValue)
                return (new QsStringCommandToken(stringResult.Value.Token), stringResult.Value.NextPos);

            // id�� �Ǵ��� �˻� (���� �Լ��� identifier�� ����Ѵ�)
            var idResult = await normalLexer.LexIdentifierAsync(curPos);
            if (idResult.HasValue)
                return (new QsIdentifierCommandToken(idResult.Value.Token), idResult.Value.NextPos);

            // id�� �ƴ�, $�� �����ϴ� �ؽ�Ʈ
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

            // 1. ��ŵ�ϰ� ���� ���� �����ߴ�
            if (curPos.IsReachEnd())
                return (new QsEndOfCommandArgToken(), curPos);

            // 2. ��ŵ�ϰ� ���� �ٹٲ��̴�
            if (curPos.GetUnicodeCategory() == UnicodeCategory.LineSeparator)
            {
                var nextLineSepPos = await curPos.NextAsync();
                return (new QsEndOfCommandArgToken(), nextLineSepPos);
            }

            // " ���� �����ϴ°� ���� Ȯ��
            var stringResult = await normalLexer.LexStringAsync(curPos);
            if (stringResult.HasValue)
                return (new QsStringCommandArgToken(stringResult.Value.Token), stringResult.Value.NextPos);

            // id�� �ƴ�, $�� �����ϴ� �ؽ�Ʈ
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
                // ���⸦ ������ �ߴ�
                if (curPos.IsWhiteSpace()) break;

                // ù���ڰ� DOLLAR �϶�
                if (curPos.Equals('$'))
                {
                    var secondPos = await curPos.NextAsync();

                    // �ι�° ���ڵ� $��� $
                    if (!secondPos.IsReachEnd() && secondPos.Equals('$')) // TODO: IsReachEnd �����ϱ�
                    {
                        sb.Append('$');
                        curPos = await secondPos.NextAsync();
                        continue;
                    }

                    // id����
                    var idResult = await normalLexer.LexIdentifierAsync(secondPos);
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
                    if (!secondPos.IsReachEnd() && secondPos.Equals('{')) // TODO: IsReachEnd �����ϱ�
                    {
                        curPos = await secondPos.NextAsync();

                        // sb �ݰ�
                        if (sb.Length != 0)
                        {
                            elems.Add(new QsTextStringTokenElement(sb.ToString()));
                            sb.Clear();
                        }

                        while (!curPos.IsReachEnd() && !curPos.Equals('}'))
                        {
                            var tokenResult = await normalLexer.GetNextTokenAsync(curPos);

                            // ��ū ��⿡ �����ߴٸ� invalid
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