using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public abstract class QsCaptureInfoLocation
    {
        public static QsCaptureInfoLocation Make(QsStmt stmt) => new QsStmtLocation(stmt);
        public static QsCaptureInfoLocation Make(QsExp exp) => new QsExpLocation(exp);

        class QsStmtLocation : QsCaptureInfoLocation
        {
            public QsStmt Stmt { get; }
            public QsStmtLocation(QsStmt stmt) { Stmt = stmt; }

            public override bool Equals(object? obj)
            {
                return obj is QsStmtLocation location &&
                       ReferenceEquals(Stmt, location.Stmt);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Stmt);
            }

            public static bool operator ==(QsStmtLocation? left, QsStmtLocation? right)
            {
                return EqualityComparer<QsStmtLocation?>.Default.Equals(left, right);
            }

            public static bool operator !=(QsStmtLocation? left, QsStmtLocation? right)
            {
                return !(left == right);
            }
        }

        class QsExpLocation : QsCaptureInfoLocation
        {
            public QsExp Exp { get; }

            public QsExpLocation(QsExp exp)
            {
                Exp = exp;
            }

            public override bool Equals(object? obj)
            {
                return obj is QsExpLocation location &&
                       ReferenceEquals(Exp, location.Exp);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Exp);
            }

            public static bool operator ==(QsExpLocation? left, QsExpLocation? right)
            {
                return EqualityComparer<QsExpLocation?>.Default.Equals(left, right);
            }

            public static bool operator !=(QsExpLocation? left, QsExpLocation? right)
            {
                return !(left == right);
            }
        }

    }

    
}
