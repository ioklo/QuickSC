using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsScriptElement
    {

    }

    public class QsStatementScriptElement : QsScriptElement
    {
        public QsStatement Stmt { get; }
        public QsStatementScriptElement(QsStatement stmt)
        {
            Stmt = stmt;
        }
    }

    // 가장 외곽
    public class QsScript
    {
        public List<QsScriptElement> Elements { get; }
        public QsScript(List<QsScriptElement> elements)
        {
            Elements = elements;
        }
    }
}