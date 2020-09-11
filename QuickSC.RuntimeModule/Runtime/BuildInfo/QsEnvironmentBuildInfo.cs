using Gum.CompileTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Gum.Runtime
{
    class QsEnvironmentBuildInfo : QsRuntimeModuleTypeBuildInfo.Class
    {
        public QsEnvironmentBuildInfo()
            : base(null, ModuleItemId.Make("Environment"), Enumerable.Empty<string>(), null, () => new ObjectValue(null))
        {
        }

        public override void Build(QsRuntimeModuleTypeBuilder builder)
        {
            var stringTypeValue = TypeValue.MakeNormal(QsRuntimeModule.StringId);

            builder.AddMemberVar(Name.MakeText("HomeDir"), false, stringTypeValue);
            builder.AddMemberVar(Name.MakeText("ScriptDir"), false, stringTypeValue);
        }
    }

    class QsEnvironmentObject : GumObject
    {
        Value homeDir;
        Value scriptDir;

        public string this[string varName]
        {
            get { return Environment.GetEnvironmentVariable(varName); }
            set { Environment.SetEnvironmentVariable(varName, value); }
        }

        public QsEnvironmentObject(Value homeDir, Value scriptDir)
        {
            this.homeDir = homeDir;
            this.scriptDir = scriptDir;
        }

        public override Value GetMemberValue(Name varName)
        {
            if (varName.Text == "HomeDir")
                return homeDir;

            if (varName.Text == "ScriptDir")
                return scriptDir;

            throw new InvalidOperationException();
        }
    }
}
