using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace QuickSC
{
    public class QsCmdCommandProvider : IQsCommandProvider
    {
        public void Execute(string cmdText)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/c " + cmdText;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();
        }
    }
}
