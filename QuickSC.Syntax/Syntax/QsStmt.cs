using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{
    public abstract class QsStmt
    {
    }
    
    // 명령어
    public class QsCommandStmt : QsStmt
    {
        public QsExp CommandExp { get; }
        public ImmutableArray<QsExp> ArgExps { get; }

        public QsCommandStmt(QsExp commandExp, ImmutableArray<QsExp> argExps)
        {
            CommandExp = commandExp;
            ArgExps = argExps;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsCommandStmt statement &&
                   EqualityComparer<QsExp>.Default.Equals(CommandExp, statement.CommandExp) &&
                   Enumerable.SequenceEqual(ArgExps, statement.ArgExps);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            hashCode.Add(CommandExp);

            foreach (var argExp in ArgExps)
                hashCode.Add(argExp);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsCommandStmt? left, QsCommandStmt? right)
        {
            return EqualityComparer<QsCommandStmt>.Default.Equals(left, right);
        }

        public static bool operator !=(QsCommandStmt? left, QsCommandStmt? right)
        {
            return !(left == right);
        }
    }

    public struct QsVarDeclStmtElement
    {
        public string VarName { get; }
        public QsExp? InitExp { get; }

        public QsVarDeclStmtElement(string varName, QsExp? initExp)
        {
            VarName = varName;
            InitExp = initExp;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDeclStmtElement element &&
                   VarName == element.VarName &&
                   EqualityComparer<QsExp?>.Default.Equals(InitExp, element.InitExp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VarName, InitExp);
        }

        public static bool operator ==(QsVarDeclStmtElement left, QsVarDeclStmtElement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsVarDeclStmtElement left, QsVarDeclStmtElement right)
        {
            return !(left == right);
        }
    }

    // int a = 0, b, c;
    public class QsVarDeclStmt : QsStmt
    {
        public string TypeName { get; }
        public ImmutableArray<QsVarDeclStmtElement> Elements { get; }

        public QsVarDeclStmt(string typeName, ImmutableArray<QsVarDeclStmtElement> elems)
        {
            TypeName = typeName;
            Elements = elems;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDeclStmt stmt &&
                   TypeName == stmt.TypeName &&
                   Enumerable.SequenceEqual(Elements, stmt.Elements);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(TypeName);

            foreach (var elem in Elements)
                hashCode.Add(elem);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsVarDeclStmt? left, QsVarDeclStmt? right)
        {
            return EqualityComparer<QsVarDeclStmt>.Default.Equals(left, right);
        }

        public static bool operator !=(QsVarDeclStmt? left, QsVarDeclStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsIfStmt : QsStmt
    {
        public QsExp CondExp { get; }
        public QsStmt BodyStmt { get; }
        public QsStmt? ElseBodyStmt { get; }

        public QsIfStmt(QsExp condExp, QsStmt bodyStmt, QsStmt? elseBodyStmt)
        {
            CondExp = condExp;
            BodyStmt = bodyStmt;
            ElseBodyStmt = elseBodyStmt;
        }
    }

    public abstract class QsForStmtInitializer { }
    public class QsExpForStmtInitializer : QsForStmtInitializer 
    { 
        public QsExp Exp { get; }
        public QsExpForStmtInitializer(QsExp exp) { Exp = exp; }
    }
    public class QsVarDeclForStmtInitializer : QsForStmtInitializer 
    {
        public QsVarDeclStmt Stmt { get; }
        public QsVarDeclForStmtInitializer(QsVarDeclStmt stmt) { Stmt = stmt; }
    }

    public class QsForStmt : QsStmt
    {
        // InitExp Or VarDecl
        public QsForStmtInitializer? Initializer { get; }
        public QsExp? CondExp { get; }
        public QsExp? ContinueExp { get; }
        public QsStmt BodyStmt { get; }

        public QsForStmt(QsForStmtInitializer? initializer, QsExp? condExp, QsExp? continueExp, QsStmt bodyStmt)
        {
            Initializer = initializer;
            CondExp = condExp;
            ContinueExp = continueExp;
            BodyStmt = bodyStmt;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsForStmt stmt &&
                   EqualityComparer<QsForStmtInitializer?>.Default.Equals(Initializer, stmt.Initializer) &&
                   EqualityComparer<QsExp?>.Default.Equals(CondExp, stmt.CondExp) &&
                   EqualityComparer<QsExp?>.Default.Equals(ContinueExp, stmt.ContinueExp) &&
                   EqualityComparer<QsStmt>.Default.Equals(BodyStmt, stmt.BodyStmt);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Initializer, CondExp, ContinueExp, BodyStmt);
        }

        public static bool operator ==(QsForStmt? left, QsForStmt? right)
        {
            return EqualityComparer<QsForStmt>.Default.Equals(left, right);
        }

        public static bool operator !=(QsForStmt? left, QsForStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsBlankStmt : QsStmt 
    {
        public static QsBlankStmt Instance { get; } = new QsBlankStmt();
        private QsBlankStmt() { }
    }

    public class QsContinueStmt : QsStmt
    {
        public static QsContinueStmt Instance { get; } = new QsContinueStmt();
        private QsContinueStmt() { }
    }

    public class QsBreakStmt : QsStmt
    {
        public static QsBreakStmt Instance { get; } = new QsBreakStmt();
        private QsBreakStmt() { }
    }

    public class QsBlockStmt : QsStmt
    {
        public ImmutableArray<QsStmt> Stmts { get; }
        public QsBlockStmt(ImmutableArray<QsStmt> stmts)
        {
            Stmts = stmts;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsBlockStmt stmt && Enumerable.SequenceEqual(Stmts, stmt.Stmts);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            foreach (var stmt in Stmts)
                hashCode.Add(stmt);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsBlockStmt? left, QsBlockStmt? right)
        {
            return EqualityComparer<QsBlockStmt>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBlockStmt? left, QsBlockStmt? right)
        {
            return !(left == right);
        }
    }
    
    public class QsExpStmt : QsStmt
    {
        public QsExp Exp { get; }
        public QsExpStmt(QsExp exp)
        { 
            Exp = exp; 
        }

        public override bool Equals(object? obj)
        {
            return obj is QsExpStmt stmt &&
                   EqualityComparer<QsExp>.Default.Equals(Exp, stmt.Exp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Exp);
        }

        public static bool operator ==(QsExpStmt? left, QsExpStmt? right)
        {
            return EqualityComparer<QsExpStmt>.Default.Equals(left, right);
        }

        public static bool operator !=(QsExpStmt? left, QsExpStmt? right)
        {
            return !(left == right);
        }
    }
}