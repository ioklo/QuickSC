using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QuickSC.Syntax
{
    public abstract class QsStatement
    {

    }

    // 명령어
    public class QsCommandStatement : QsStatement
    {
        public QsExp CommandExp { get; }
        public ImmutableArray<QsExp> ArgExps { get; }

        public QsCommandStatement(QsExp commandExp, ImmutableArray<QsExp> argExps)
        {
            CommandExp = commandExp;
            ArgExps = argExps;
        }

        public override bool Equals(object? obj)
        {
            return obj is QsCommandStatement statement &&
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

        public static bool operator ==(QsCommandStatement? left, QsCommandStatement? right)
        {
            return EqualityComparer<QsCommandStatement>.Default.Equals(left, right);
        }

        public static bool operator !=(QsCommandStatement? left, QsCommandStatement? right)
        {
            return !(left == right);
        }
    }
}