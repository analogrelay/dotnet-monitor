using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Internal.Utilities;

namespace Microsoft.Diagnostics.Tools.Analyze
{
    [Command(Name = "dotnet-analyze", Description = "Analyzes a crash dump for known issues")]
    internal class Program
    {
        private static int Main(string[] args)
        {
            DebugUtil.WaitForDebuggerIfRequested(ref args);

            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
        }

        public int OnExecute(IConsole console, CommandLineApplication app)
        {
            console.WriteLine("You seem to have had a bad problem and will not go to space today.");
            return 0;
        }
    }
}
