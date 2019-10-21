using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace WamWooWam.Core
{
#if !NETSTANDARD1_6 && !NET40
    public static class ProcessExtensions
    {
        public static async Task<string> RunAndGetStdoutAsync(this ProcessStartInfo proc)
        {
            var builder = new StringBuilder();
            proc.RedirectStandardOutput = true;
            proc.UseShellExecute = false;

            var p = Process.Start(proc);

            p.OutputDataReceived += (o, e) => builder.AppendLine(e.Data);
            p.BeginOutputReadLine();

            await Task.Run(() => p.WaitForExit());

            return builder.ToString();
        }
    }
#endif
}
