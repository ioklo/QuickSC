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
        public QsTypeValue? SeqElemTypeValue { get; }  // seqCall이 아니라면 null이다
        public override bool bThisCall { get; }        // Caller입장에서 this를 전달할지
        public QsValue? CapturedThis { get; } // 캡쳐한 곳에 있던 this를 쓸지
        public ImmutableArray<QsValue> Captures { get; } // LocalIndex 0 부터.. 그 뒤에 argument가 붙는다
        public int LocalVarCount { get; }
        public QsStmt Body { get; }

        public QsScriptFuncInst(QsTypeValue? seqElemTypeValue, bool bThisCall, QsValue? capturedThis, ImmutableArray<QsValue> captures, int localVarCount, QsStmt body)
        {
            // 둘 중 하나는 false여야 한다
            Debug.Assert(!bThisCall || capturedThis == null);

            SeqElemTypeValue = seqElemTypeValue;
            this.bThisCall = bThisCall;
            CapturedThis = capturedThis;
            Captures = captures;
            LocalVarCount = localVarCount;
            Body = body;
        }
    }
}
