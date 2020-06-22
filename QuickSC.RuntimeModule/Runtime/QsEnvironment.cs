using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace QuickSC.Runtime
{
    class QsEnvironmentInfo : QsRuntimeModuleObjectInfo
    {
        public QsEnvironmentInfo()
        {
            var typeId = new QsMetaItemId(new QsMetaItemIdElem("Environment"));

            var memberVarIdsBuilder = ImmutableArray.CreateBuilder<QsMetaItemId>();

            var stringTypeValue = new QsTypeValue_Normal(null, new QsMetaItemId(new QsMetaItemIdElem("string")));

            var homeDirId = typeId.Append("HomeDir");
            AddNativeVar(new QsNativeVar(homeDirId, stringTypeValue));
            memberVarIdsBuilder.Add(homeDirId);

            var scriptDirId = typeId.Append("ScriptDir");
            AddNativeVar(new QsNativeVar(scriptDirId, stringTypeValue));
            memberVarIdsBuilder.Add(scriptDirId);

            AddNativeType(new QsNativeType(typeId, ImmutableArray<string>.Empty, baseTypeValue: null,
                memberTypeIds: ImmutableArray<QsMetaItemId>.Empty,
                staticMemberFuncIds: ImmutableArray<QsMetaItemId>.Empty,
                staticMemberVarIds: ImmutableArray<QsMetaItemId>.Empty,
                memberFuncIds: ImmutableArray<QsMetaItemId>.Empty,
                memberVarIds: memberVarIdsBuilder.ToImmutable(),
                new QsNativeTypeInstantiator(() => new QsObjectValue(null))));
        }
    }

    class QsEnvironmentObject : QsObject
    {
        QsValue homeDir;
        QsValue scriptDir;

        public string this[string varName]
        {
            get { return Environment.GetEnvironmentVariable(varName); }
            set { Environment.SetEnvironmentVariable(varName, value); }
        }

        public QsEnvironmentObject(QsValue homeDir, QsValue scriptDir)
        {
            this.homeDir = homeDir;
            this.scriptDir = scriptDir;
        }

        public override QsValue GetMemberValue(QsName varName)
        {
            if (varName.Name == "HomeDir")
                return homeDir;

            if (varName.Name == "ScriptDir")
                return scriptDir;

            throw new InvalidOperationException();
        }
    }
}
