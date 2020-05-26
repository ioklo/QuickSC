using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{
    public struct QsCaptureElem
    {
        public QsStorage Storage { get;} 
        public QsCaptureKind Kind { get; }
        public string Name { get; }

        public QsCaptureElem(QsStorage storage, QsCaptureKind kind, string name)
        {
            Storage = storage;
            Kind = kind;
            Name = name;
        }
    }

    public class QsCaptureInfo
    {
        public bool bCaptureThis { get; }
        public ImmutableArray<QsCaptureElem> Captures { get; }

        public QsCaptureInfo(bool bCaptureThis, ImmutableArray<QsCaptureElem> captures)
        {
            this.bCaptureThis = bCaptureThis;
            this.Captures = captures;
        }
    }
}
