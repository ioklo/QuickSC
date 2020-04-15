using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsScriptElement
    {
    }

    public class QsStmtScriptElement : QsScriptElement
    {
        public QsStmt Stmt { get; }
        public QsStmtScriptElement(QsStmt stmt)
        {
            Stmt = stmt;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsStmtScriptElement element &&
                   EqualityComparer<QsStmt>.Default.Equals(Stmt, element.Stmt);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Stmt);
        }

        public static bool operator ==(QsStmtScriptElement? left, QsStmtScriptElement? right)
        {
            return EqualityComparer<QsStmtScriptElement>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStmtScriptElement? left, QsStmtScriptElement? right)
        {
            return !(left == right);
        }
    }
}