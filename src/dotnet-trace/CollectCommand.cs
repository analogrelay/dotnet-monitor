using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
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

        [Option("-s|--server <SERVER>", Description = "The server to connect to, in the form of '<port>' (for localhost) or '<host>:<port>'.")]
        public string Target { get; }

        [Option("-p|--process <PID>", Description = "The process ID of the process to connect to.")]
        public int? ProcessId { get; }

        [Argument(0, "<PROVIDERS>", "The providers to collect from.")]
        public IList<string> Providers { get; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            System.Threading.CancellationToken cancellationToken = console.GetCtrlCToken();

            EndPoint endPoint;
            if (ProcessId is int pid)
            {
                ProcessRegistration reg = await ProcessLocator.GetRegistrationAsync(pid);
                endPoint = new IPEndPoint(IPAddress.Loopback, reg.DiagnosticsPort);
            }
            else
            {
                if (string.IsNullOrEmpty(Target))
                {
                    console.Error.WriteLine("Missing required option: --server");
                    return 1;
                }

                if (!EndPointParser.TryParseEndpoint(Target, out endPoint))
                {
                    console.Error.WriteLine($"Invalid server value: {Target}");
                    return 1;
                }
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
                var formattedMessage = string.Format(evt.Message, evt.Payload.ToArray());
                console.WriteLine($"{evt.ProviderName}/{evt.EventName}({evt.EventId}): {formattedMessage}");
            };

            await client.ConnectAsync();

            await client.EnableEventsAsync(Providers.Select(p => new EnableEventsRequest(p, EventLevel.Verbose, EventKeywords.All)));

            console.WriteLine("Connected, press Ctrl-C to terminate...");
            await cancellationToken.WaitForCancellationAsync();

            return 0;
        }
    }
}
