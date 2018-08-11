using System;
using System.Threading;
using McMaster.Extensions.CommandLineUtils;

namespace Microsoft.Diagnostics.Tools.Trace
{
    public static class ConsoleCancellationExtensions
    {
        public static CancellationToken GetCtrlCToken(this IConsole console)
        {
            var cts = new CancellationTokenSource();
            console.CancelKeyPress += (sender, args) =>
            {
                if (cts.IsCancellationRequested)
                {
                    // Terminate forcibly, the user pressed Ctrl-C a second time
                    args.Cancel = false;
                }
                else
                {
                    // Don't terminate, just trip the token
                    args.Cancel = true;
                    cts.Cancel();
                }
            };
            return cts.Token;
        }
    }
}
