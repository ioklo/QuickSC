using QuickSC.StaticAnalyzer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Syntax
{
    // 추가 exp

    public abstract class QsEvalExp
    {

    }

    public class QsGlobalIdExp : QsEvalExp
    {
        public QsVarId VarId { get; }
        public QsGlobalIdExp(QsVarId varId) { VarId = varId; }
    }

    public class QsLocalIdExp : QsEvalExp
    {
        public int LocalIndex { get; }
        public QsLocalIdExp(int localIndex) { LocalIndex = localIndex; }
    }

    public class QsLambdaEvalExp : QsEvalExp
    {
        public struct Elem
        {
            public QsCaptureKind Kind { get; }
            public QsEvalExp EvalExp { get; }

            public Elem(QsCaptureKind kind, QsEvalExp evalExp)
            {
                Kind = kind;
                EvalExp = evalExp;
            }
        }

        public bool bCaptureThis { get; }

        // 캡쳐 변수들
        public ImmutableArray<Elem> CaptureElems { get; }

        public QsLambdaEvalExp(bool bCaptureThis, ImmutableArray<Elem> captureElems)
        {
            this.bCaptureThis = bCaptureThis;
            CaptureElems = captureElems;
        }
    }
}
