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

}