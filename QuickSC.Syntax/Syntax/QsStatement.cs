using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsStatement
    {

    }

    // 명령어
    public class QsCommandStatement : QsStatement
    {
        public QsExp CommandExp { get; }
        public List<QsExp> ArgExps { get; }

        public QsCommandStatement(QsExp commandExp, List<QsExp> argExps)
        {
            CommandExp = commandExp;
            ArgExps = argExps;
        }
    }
}