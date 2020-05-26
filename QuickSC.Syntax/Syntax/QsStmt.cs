using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.XPath;

namespace QuickSC.Syntax
{
    public abstract class QsStmt : IQsSyntaxNode
    {
    }
    
    // 명령어
    public class QsCommandStmt : QsStmt
    {
        public ImmutableArray<QsStringExp> Commands { get; }

        public QsCommandStmt(ImmutableArray<QsStringExp> commands)
        {
            Debug.Assert(0 < commands.Length);
            Commands = commands;
        }

        public QsCommandStmt(params QsStringExp[] commands)
        {
            Debug.Assert(0 < commands.Length);
            Commands = ImmutableArray.Create(commands);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsCommandStmt statement &&
                   Enumerable.SequenceEqual(Commands, statement.Commands);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            foreach (var command in Commands)
                hashCode.Add(command);

            return hashCode.ToHashCode();
        }

        public static bool operator ==(QsCommandStmt? left, QsCommandStmt? right)
        {
            return EqualityComparer<QsCommandStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsCommandStmt? left, QsCommandStmt? right)
        {
            return !(left == right);
        }
    }

    public struct QsVarDeclElement
    {
        public string VarName { get; }
        public QsExp? InitExp { get; }

        public QsVarDeclElement(string varName, QsExp? initExp)
        {
            VarName = varName;
            InitExp = initExp;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDeclElement element &&
                   VarName == element.VarName &&
                   EqualityComparer<QsExp?>.Default.Equals(InitExp, element.InitExp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VarName, InitExp);
        }

        public static bool operator ==(QsVarDeclElement left, QsVarDeclElement right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QsVarDeclElement left, QsVarDeclElement right)
        {
            return !(left == right);
        }
    }

    public class QsVarDecl
    {
        public QsTypeExp Type { get; }
        public ImmutableArray<QsVarDeclElement> Elements { get; }

        public QsVarDecl(QsTypeExp type, ImmutableArray<QsVarDeclElement> elems)
        {
            Type = type;
            Elements = elems;
        }

        public QsVarDecl(QsTypeExp type, params QsVarDeclElement[] elems)
        {
            Type = type;
            Elements = ImmutableArray.Create(elems);
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDecl decl &&
                   EqualityComparer<QsTypeExp>.Default.Equals(Type, decl.Type) &&
                   Enumerable.SequenceEqual(Elements, decl.Elements);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Elements);
        }

        public static bool operator ==(QsVarDecl? left, QsVarDecl? right)
        {
            return EqualityComparer<QsVarDecl?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsVarDecl? left, QsVarDecl? right)
        {
            return !(left == right);
        }
    }

    // int a = 0, b, c;
    public class QsVarDeclStmt : QsStmt
    {
        public QsVarDecl VarDecl { get; }

        public QsVarDeclStmt(QsVarDecl varDecl)
        {
            VarDecl = varDecl;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDeclStmt stmt &&
                   EqualityComparer<QsVarDecl>.Default.Equals(VarDecl, stmt.VarDecl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VarDecl);
        }

        public static bool operator ==(QsVarDeclStmt? left, QsVarDeclStmt? right)
        {
            return EqualityComparer<QsVarDeclStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsVarDeclStmt? left, QsVarDeclStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsIfStmt : QsStmt
    {
        public QsExp Cond { get; }
        public QsTypeExp? TestType { get; }
        public QsStmt Body { get; }
        public QsStmt? ElseBody { get; }

        public QsIfStmt(QsExp cond, QsTypeExp? testType, QsStmt body, QsStmt? elseBody)
        {
            Cond = cond;
            TestType = testType;
            Body = body;
            ElseBody = elseBody;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsIfStmt stmt &&
                   EqualityComparer<QsExp>.Default.Equals(Cond, stmt.Cond) &&
                   EqualityComparer<QsTypeExp?>.Default.Equals(TestType, stmt.TestType) &&
                   EqualityComparer<QsStmt>.Default.Equals(Body, stmt.Body) &&
                   EqualityComparer<QsStmt?>.Default.Equals(ElseBody, stmt.ElseBody);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cond, Body, ElseBody);
        }

        public static bool operator ==(QsIfStmt? left, QsIfStmt? right)
        {
            return EqualityComparer<QsIfStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsIfStmt? left, QsIfStmt? right)
        {
            return !(left == right);
        }
    }

    
    public abstract class QsForStmtInitializer { }
    public class QsExpForStmtInitializer : QsForStmtInitializer
    {
        public QsExp Exp { get; }
        public QsExpForStmtInitializer(QsExp exp) { Exp = exp; }

        public override bool Equals(object? obj)
        {
            return obj is QsExpForStmtInitializer initializer &&
                   EqualityComparer<QsExp>.Default.Equals(Exp, initializer.Exp);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Exp);
        }

        public static bool operator ==(QsExpForStmtInitializer? left, QsExpForStmtInitializer? right)
        {
            return EqualityComparer<QsExpForStmtInitializer?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsExpForStmtInitializer? left, QsExpForStmtInitializer? right)
        {
            return !(left == right);
        }
    }
    public class QsVarDeclForStmtInitializer : QsForStmtInitializer
    {
        public QsVarDecl VarDecl { get; }
        public QsVarDeclForStmtInitializer(QsVarDecl varDecl) { VarDecl = varDecl; }

        public override bool Equals(object? obj)
        {
            return obj is QsVarDeclForStmtInitializer initializer &&
                   EqualityComparer<QsVarDecl>.Default.Equals(VarDecl, initializer.VarDecl);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VarDecl);
        }

        public static bool operator ==(QsVarDeclForStmtInitializer? left, QsVarDeclForStmtInitializer? right)
        {
            return EqualityComparer<QsVarDeclForStmtInitializer?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsVarDeclForStmtInitializer? left, QsVarDeclForStmtInitializer? right)
        {
            return !(left == right);
        }
    }

    public class QsForStmt : QsStmt
    {
        // InitExp Or VarDecl
        public QsForStmtInitializer? Initializer { get; }
        public QsExp? CondExp { get; }
        public QsExp? ContinueExp { get; }
        public QsStmt Body { get; }

        public QsForStmt(QsForStmtInitializer? initializer, QsExp? condExp, QsExp? continueExp, QsStmt bodyStmt)
        {
            Initializer = initializer;
            CondExp = condExp;
            ContinueExp = continueExp;
            Body = bodyStmt;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsForStmt stmt &&
                   EqualityComparer<QsForStmtInitializer?>.Default.Equals(Initializer, stmt.Initializer) &&
                   EqualityComparer<QsExp?>.Default.Equals(CondExp, stmt.CondExp) &&
                   EqualityComparer<QsExp?>.Default.Equals(ContinueExp, stmt.ContinueExp) &&
                   EqualityComparer<QsStmt>.Default.Equals(Body, stmt.Body);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Initializer, CondExp, ContinueExp, Body);
        }

        public static bool operator ==(QsForStmt? left, QsForStmt? right)
        {
            return EqualityComparer<QsForStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsForStmt? left, QsForStmt? right)
        {
            return !(left == right);
        }
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

    public class QsReturnStmt : QsStmt
    {
        public QsExp? Value { get; }
        public QsReturnStmt(QsExp? value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsReturnStmt stmt &&
                   EqualityComparer<QsExp?>.Default.Equals(Value, stmt.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsReturnStmt? left, QsReturnStmt? right)
        {
            return EqualityComparer<QsReturnStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsReturnStmt? left, QsReturnStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsBlockStmt : QsStmt
    {
        public ImmutableArray<QsStmt> Stmts { get; }
        public QsBlockStmt(ImmutableArray<QsStmt> stmts)
        {
            Stmts = stmts;
        }

        public QsBlockStmt(params QsStmt[] stmts)
        {
            Stmts = ImmutableArray.Create(stmts);
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
            return EqualityComparer<QsBlockStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsBlockStmt? left, QsBlockStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsBlankStmt : QsStmt
    {
        public static QsBlankStmt Instance { get; } = new QsBlankStmt();
        private QsBlankStmt() { }
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
            return EqualityComparer<QsExpStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsExpStmt? left, QsExpStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsTaskStmt : QsStmt
    {
        public QsStmt Body { get; }
        public QsTaskStmt(QsStmt body) { Body = body; }
    }

    public class QsAwaitStmt : QsStmt
    {
        public QsStmt Body { get; }
        public QsAwaitStmt(QsStmt body) { Body = body; }
    }

    public class QsAsyncStmt : QsStmt
    {
        public QsStmt Body { get; }
        public QsAsyncStmt(QsStmt body) { Body = body; }
    }

    public class QsForeachStmt : QsStmt
    {
        public QsTypeExp Type { get; }
        public string VarName { get; }
        public QsExp Obj { get; }
        public QsStmt Body { get; }

        public QsForeachStmt(QsTypeExp type, string varName, QsExp obj, QsStmt body)
        {
            Type = type;
            VarName = varName;
            Obj = obj;
            Body = body;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsForeachStmt stmt &&
                   EqualityComparer<QsTypeExp>.Default.Equals(Type, stmt.Type) &&
                   VarName == stmt.VarName &&
                   EqualityComparer<QsExp>.Default.Equals(Obj, stmt.Obj) &&
                   EqualityComparer<QsStmt>.Default.Equals(Body, stmt.Body);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, VarName, Obj, Body);
        }

        public static bool operator ==(QsForeachStmt? left, QsForeachStmt? right)
        {
            return EqualityComparer<QsForeachStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsForeachStmt? left, QsForeachStmt? right)
        {
            return !(left == right);
        }
    }

    public class QsYieldStmt : QsStmt
    {
        public QsExp Value { get; }
        public QsYieldStmt(QsExp value) { Value = value; }

        public override bool Equals(object? obj)
        {
            return obj is QsYieldStmt stmt &&
                   EqualityComparer<QsExp>.Default.Equals(Value, stmt.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(QsYieldStmt? left, QsYieldStmt? right)
        {
            return EqualityComparer<QsYieldStmt?>.Default.Equals(left, right);
        }

        public static bool operator !=(QsYieldStmt? left, QsYieldStmt? right)
        {
            return !(left == right);
        }
    }
}