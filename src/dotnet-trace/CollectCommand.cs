using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Diagnostics.Client;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Tools.Trace
{
    [Command(Name = Name, Description = "Collects events from the target process")]
    public class CollectCommand
    {
        public const string Name = "collect";

        [Option("-s|--server <SERVER>", Description = "The server to connect to, in the form of '<port>' (for localhost) or '<host>:<port>'")]
        public string Target { get; }

        [Argument(0, "<PROVIDERS>", "The providers to collect from")]
        public IList<string> Providers { get; }
        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            var cancellationToken = console.GetCtrlCToken();

            if (string.IsNullOrEmpty(Target))
            {
                console.Error.WriteLine("Missing required option: --server");
                return 1;
            }

            if (!EndPointParser.TryParseEndpoint(Target, out var endPoint))
            {
                console.Error.WriteLine($"Invalid server value: {Target}");
                return 1;
            }

            if (Providers.Count == 0)
            {
                console.Error.WriteLine("No providers were listed");
                return 1;
            }

            var client = new DiagnosticsClient(endPoint);

            console.WriteLine("Connecting to application...");

            client.OnEventWritten += (evt) =>
            {
                console.WriteLine($"{evt.ProviderName}/{evt.EventName}({evt.EventId})");
            };

            await client.ConnectAsync();

            await client.EnableEventsAsync(Providers.Select(p => new EnableEventsRequest(p, EventLevel.Verbose, EventKeywords.All)));

            console.WriteLine("Connected, press Ctrl-C to terminate...");
            await cancellationToken.WaitForCancellationAsync();

            return 0;
        }
    }
}
