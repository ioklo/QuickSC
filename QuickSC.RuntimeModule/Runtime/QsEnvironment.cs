using System;
using System.Collections.Generic;
using System.Text;

namespace QuickSC.Runtime
{
    class QsEnvironment
    {
        // IQsCommandProvider CommandProvider;
        public string HomeDir;
        public string ScriptDir;

        public string this[string varName]
        {
            get { return Environment.GetEnvironmentVariable(varName); }
            set { Environment.SetEnvironmentVariable(varName, value); }
        }

        public QsEnvironment(string homeDir, string scriptDir)
        {
            HomeDir = homeDir;
            ScriptDir = scriptDir;
        }
    }
}
