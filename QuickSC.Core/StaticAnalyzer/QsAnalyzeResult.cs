using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    class QsAnalyzeResultBuilder
    {
        ImmutableList<(object Obj, string Message)>.Builder errors;

        public QsAnalyzeResultBuilder()
        {
            errors = ImmutableList.CreateBuilder<(object Obj, string Message)>();
        }

        public void AddResult(QsAnalyzeResult result)
        {
            errors.AddRange(result.Errors);
        }

        public void AddError(object obj, string msg)
        {
            errors.Add((obj, msg));
        }

        public QsAnalyzeResult ToResult()
        {
            return new QsAnalyzeResult(errors.ToImmutableList());
        }
    }

    struct QsAnalyzeResult
    {
        public ImmutableList<(object Obj, string Message)> Errors { get; }

        public static QsAnalyzeResult OK { get; }
        public static QsAnalyzeResult Error(object obj, string msg)
        {
            return new QsAnalyzeResult(ImmutableList<(object Obj, string Message)>.Empty.Add((obj, msg)));
        }

        public QsAnalyzeResult(ImmutableList<(object Obj, string Message)> errors)
        {
            Errors = errors;
        }
    }

    struct QsExpAnalyzeResult
    {
        public QsAnalyzeResult Result { get; }
        public QsTypeValue Type { get; }
    }
}
