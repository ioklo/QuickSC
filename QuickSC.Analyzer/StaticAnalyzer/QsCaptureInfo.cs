using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.StaticAnalyzer
{   
    public class QsCaptureInfo
    {
        public struct Element
        {   
            public QsCaptureKind CaptureKind { get; }
            public QsStorageInfo StorageInfo { get; }

            public Element(QsCaptureKind captureKind, QsStorageInfo storageInfo)
            {
                CaptureKind = captureKind;
                StorageInfo = storageInfo;
            }
        }

        public bool bCaptureThis { get; }
        public ImmutableArray<Element> Captures { get; }

        public QsCaptureInfo(bool bCaptureThis, ImmutableArray<Element> captures)
        {
            this.bCaptureThis = bCaptureThis;
            this.Captures = captures;
        }
    }
}
