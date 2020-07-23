using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Runtime
{
    class QsEnvironmentInfo : QsRuntimeModuleObjectInfo
    {
        public QsEnvironmentInfo()
            : base(null, new QsMetaItemId(new QsMetaItemIdElem("Environment")), Enumerable.Empty<string>(), null, () => new QsObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleObjectBuilder builder)
        {
            var stringTypeValue = new QsTypeValue_Normal(null, QsRuntimeModule.StringId);

            builder.AddMemberVar(QsName.Text("HomeDir"), false, stringTypeValue);
            builder.AddMemberVar(QsName.Text("ScriptDir"), false, stringTypeValue);
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
