using System;
using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Internal.Utilities;

namespace Microsoft.Diagnostics.Tools.Analyze
{
    [Command(Name = "dotnet-analyze", Description = "Analyzes a crash dump for known issues")]
    internal class Program
    {
        [FileExists(ErrorMessage = "The dump file could not be found.")]
        [Required(ErrorMessage = "You must provide a dump file to be analyzed.")]
        [Argument(0, "<DUMP>", Description = "The path to the dump file to analyze.")]
        public string DumpPath { get; set;}

        public int OnExecute(IConsole console, CommandLineApplication app)
        {
            console.WriteLine("You seem to have had a bad problem and will not go to space today.");
            return 0;
        }

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
    }
}
