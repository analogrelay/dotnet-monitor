using System;
using System.Diagnostics;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Microsoft.Diagnostics.Tools.Trace
{
    [Command(Name = "dotnet-trace", Description = "Collects event traces from .NET processes")]
    [Subcommand(SourcesCommand.Name, typeof(SourcesCommand))]
    [Subcommand(CollectCommand.Name, typeof(CollectCommand))]
    internal class Program
    {
        private static int Main(string[] args)
        {
#if DEBUG
            if (args.Any(a => a == "--debug"))
            {
                args = args.Where(a => a != "--debug").ToArray();
                Console.WriteLine($"Ready for debugger to attach. Process ID: {Process.GetCurrentProcess().Id}.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }
#endif

            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
        }

        public int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 0;
        }
    }
}
