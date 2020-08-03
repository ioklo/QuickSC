using QuickSC.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC
{
    public abstract class QsStorageInfo
    {
        public class ModuleGlobal : QsStorageInfo
        {
            public QsMetaItemId VarId { get; }
            internal ModuleGlobal(QsMetaItemId varId) { VarId = varId; }
        }

        public class PrivateGlobal : QsStorageInfo
        {
            public int Index { get; }
            public PrivateGlobal(int index) { Index = index; }
        }

        public class Local : QsStorageInfo
        {
            public int Index { get; }
            public Local(int localIndex) { Index = localIndex; }
        }

        public class StaticMember : QsStorageInfo
        {
            public QsExp? ObjectExp { get; }
            public QsVarValue VarValue { get; }
            public StaticMember(QsExp? objectExp, QsVarValue varValue) { ObjectExp = objectExp; VarValue = varValue; }
        }

        public class InstanceMember : QsStorageInfo
        {
            public QsExp ObjectExp { get; }
            public QsTypeValue ObjectTypeValue { get; }
            public QsName VarName { get; }
            public InstanceMember(QsExp objectExp, QsTypeValue objectTypeValue, QsName varName)
            {
                ObjectExp = objectExp;
                ObjectTypeValue = objectTypeValue;
                VarName = varName;
            }
        }

        public static ModuleGlobal MakeModuleGlobal(QsMetaItemId varId) 
            => new ModuleGlobal(varId);

        public static PrivateGlobal MakePrivateGlobal(int index) 
            => new PrivateGlobal(index);

        public static Local MakeLocal(int index) 
            => new Local(index);

        public static StaticMember MakeStaticMember(QsExp? objectExp, QsVarValue varValue)
            => new StaticMember(objectExp, varValue);

        public static InstanceMember MakeInstanceMember(QsExp objectExp, QsTypeValue objectTypeValue, QsName varName)
            => new InstanceMember(objectExp, objectTypeValue, varName);
    }

    
    
    // public class QsThisStorage : QsStorage
    // public class QsStaticThisStorage : QsStorage
}
