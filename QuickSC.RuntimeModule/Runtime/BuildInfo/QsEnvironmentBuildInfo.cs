using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace QuickSC.Runtime
{
    class QsEnvironmentBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsEnvironmentBuildInfo()
            : base(null, ModuleItemId.Make("Environment"), Enumerable.Empty<string>(), null, () => new QsObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            var stringTypeValue = TypeValue.MakeNormal(QsRuntimeModule.StringId);

            builder.AddMemberVar(Name.MakeText("HomeDir"), false, stringTypeValue);
            builder.AddMemberVar(Name.MakeText("ScriptDir"), false, stringTypeValue);
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

        public override QsValue GetMemberValue(Name varName)
        {
            if (varName.Text == "HomeDir")
                return homeDir;

            if (varName.Text == "ScriptDir")
                return scriptDir;

            throw new InvalidOperationException();
        }
    }
}
