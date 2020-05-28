using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public abstract class QsStorage
    {
    }

    public class QsGlobalStorage : QsStorage
    {
        public QsVarId VarId { get; }
        public QsGlobalStorage(QsVarId varId) { VarId = varId; }
    }

    public class QsLocalStorage : QsStorage
    {
        public int LocalIndex { get; }
        public QsLocalStorage(int localIndex) { LocalIndex = localIndex; }
    }

    // public class QsThisStorage : QsStorage
    // public class QsStaticThisStorage : QsStorage
}
