using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace QuickSC
{
    public class QsCmdCommandProvider : IQsCommandProvider
    {
        public void Execute(string nameStr, ImmutableArray<string> argStrs)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";

            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(nameStr);
            foreach (var argStr in argStrs)
                psi.ArgumentList.Add(argStr);

            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();
        }
    }
}
