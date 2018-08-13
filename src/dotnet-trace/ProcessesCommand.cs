using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Tools.Trace
{
    [Command(Name = Name, Description = "Lists available processes.")]
    internal class ProcessesCommand
    {
        public const string Name = "processes";

        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            var pids = await ProcessLocator.GetAllProcessIdsAsync();

            foreach(var pid in pids)
            {
                var reg = await ProcessLocator.GetRegistrationAsync(pid);

                console.WriteLine($"* {pid} {reg.ImageName} {reg.CommandLine} (port: {reg.DiagnosticsPort}, v{reg.ProtocolVersion})");
            }

            return 0;
        }
    }
}
