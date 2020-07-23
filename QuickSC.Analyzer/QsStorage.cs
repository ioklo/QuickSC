using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public abstract class QsStorage
    {
        public class ModuleGlobal : QsStorage
        {
            public QsMetaItemId VarId { get; }
            internal ModuleGlobal(QsMetaItemId varId) { VarId = varId; }
        }

        public class PrivateGlobal : QsStorage
        {
            public int Index { get; }
            public PrivateGlobal(int index) { Index = index; }
        }

        public class Local : QsStorage
        {
            public int Index { get; }
            public Local(int localIndex) { Index = localIndex; }
        }


        public static ModuleGlobal MakeModuleGlobal(QsMetaItemId varId) { return new ModuleGlobal(varId); }
        public static PrivateGlobal MakePrivateGlobal(int index) { return new PrivateGlobal(index); }

        public static Local MakeLocal(int index) { return new Local(index); }
    }

    
    
    // public class QsThisStorage : QsStorage
    // public class QsStaticThisStorage : QsStorage
}
