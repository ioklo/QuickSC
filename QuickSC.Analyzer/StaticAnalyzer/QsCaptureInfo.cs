using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public class QsCaptureInfo
    {
        public bool bCaptureThis { get; }
        public ImmutableDictionary<string, QsCaptureContextCaptureKind> Captures { get; }

        public QsCaptureInfo(bool bCaptureThis, ImmutableDictionary<string, QsCaptureContextCaptureKind> captures)
        {
            this.bCaptureThis = bCaptureThis;
            this.Captures = captures;
        }
    }
}
