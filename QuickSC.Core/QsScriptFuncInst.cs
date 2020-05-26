using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace QuickSC
{
    class QsScriptFuncInst : QsFuncInst
    {
        public bool bSeqCall { get; }
        public override bool bThisCall { get; }        // Caller입장에서 this를 전달할지
        public QsValue? CapturedThis { get; } // 캡쳐한 곳에 있던 this를 쓸지
        public ImmutableArray<QsValue> Captures { get; } // LocalIndex 0 부터.. 그 뒤에 argument가 붙는다
        public QsStmt Body { get; }

        public QsScriptFuncInst(bool bSeqCall, bool bThisCall, QsValue? capturedThis, ImmutableArray<QsValue> captures, QsStmt body)
        {
            // 둘 중 하나는 false여야 한다
            Debug.Assert(!bThisCall || capturedThis == null);

            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            CapturedThis = capturedThis;
            Captures = captures;
            Body = body;
        }
    }
}
