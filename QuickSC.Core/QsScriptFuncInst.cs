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
        public bool bThisCall { get; }        // Caller입장에서 this를 전달할지
        public QsValue? CapturedThis { get; } // 캡쳐한 곳에 있던 this를 쓸지
        public ImmutableDictionary<string, QsValue> Captures { get; }
        public ImmutableArray<string> Params { get; }
        public QsStmt Body { get; }

        public QsScriptFuncInst(bool bSeqCall, bool bThisCall, QsValue? capturedThis, ImmutableDictionary<string, QsValue> captures, ImmutableArray<string> parameters, QsStmt body)
        {
            // 둘 중 하나는 false여야 한다
            Debug.Assert(!bThisCall || capturedThis == null);

            this.bSeqCall = bSeqCall;
            this.bThisCall = bThisCall;
            CapturedThis = capturedThis;
            Captures = captures;
            Params = parameters;
            Body = body;
        }
    }
}
