using System;
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

        public override bool Equals(object? obj)
        {
            return obj is QsStatementScriptElement element &&
                   EqualityComparer<QsStatement>.Default.Equals(Stmt, element.Stmt);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Stmt);
        }

        public static bool operator ==(QsStatementScriptElement? left, QsStatementScriptElement? right)
        {
            return EqualityComparer<QsStatementScriptElement>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStatementScriptElement? left, QsStatementScriptElement? right)
        {
            return !(left == right);
        }
    }
}