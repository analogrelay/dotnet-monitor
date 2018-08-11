using System;
using System.Threading;

namespace SampleMonitoredApp
{
    public static class ConsoleCancellation
    {
        public static CancellationToken CreateCtrlCCancellationToken()
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
