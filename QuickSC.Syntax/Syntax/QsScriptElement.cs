using System;
using System.Collections.Generic;

namespace QuickSC.Syntax
{
    public abstract class QsScriptElement
    {
    }    

    public class QsFuncDeclScriptElement : QsScriptElement
    {
        public QsFuncDecl FuncDecl { get; }
        public QsFuncDeclScriptElement(QsFuncDecl funcDecl)
        {
            FuncDecl = funcDecl;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsFuncDeclScriptElement element &&
                   EqualityComparer<QsFuncDecl>.Default.Equals(FuncDecl, element.FuncDecl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FuncDecl);
        }

        public static bool operator ==(QsFuncDeclScriptElement? left, QsFuncDeclScriptElement? right)
        {
            return EqualityComparer<QsFuncDeclScriptElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsFuncDeclScriptElement? left, QsFuncDeclScriptElement? right)
        {
            return !(left == right);
        }
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
            return EqualityComparer<QsStmtScriptElement?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsStmtScriptElement? left, QsStmtScriptElement? right)
        {
            return !(left == right);
        }
    }

    public class QsEnumDeclScriptElement : QsScriptElement
    {
        public QsEnumDecl EnumDecl { get; }
        public QsEnumDeclScriptElement(QsEnumDecl enumDecl) { EnumDecl = enumDecl; }
    }
        
}