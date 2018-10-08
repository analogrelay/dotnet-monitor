using System;
using System.Threading;

namespace Microsoft.Internal.Utilities
{
    public static class ConsoleCancellationExtensions
    {
        public static CancellationToken GetCtrlCToken()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, args) =>
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
