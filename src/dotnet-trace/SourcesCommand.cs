using System.Net;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Diagnostics.Client;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Tools.Trace
{
    [Command(Name = Name, Description = "Lists Event Sources that exist in the target process, and lists new ones as they are created.")]
    internal class SourcesCommand
    {
        public const string Name = "sources";

        [Option("-s|--server <SERVER>", Description = "The server to connect to, in the form of '<port>' (for localhost) or '<host>:<port>'")]
        public string Target { get; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            var cancellationToken = console.GetCtrlCToken();

            if (string.IsNullOrEmpty(Target))
            {
                console.Error.WriteLine("Missing required option: --server");
                return 1;
            }

            if (!TryParseEndpoint(Target, out var endPoint))
            {
                console.Error.WriteLine($"Invalid server value: {Target}");
                return 1;
            }

            var client = new DiagnosticsClient(endPoint);

            console.WriteLine("Connecting to application...");
            await client.ConnectAsync();

            console.WriteLine("Connected, press Ctrl-C to terminate...");
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await client.ReceiveAsync(cancellationToken);

                if (message is EventSourceCreatedMessage eventSourceCreatedMessage)
                {
                    console.WriteLine($"* {eventSourceCreatedMessage.Name} [{eventSourceCreatedMessage.Guid}] (settings: {eventSourceCreatedMessage.Settings})");
                }
            }

            return 0;
        }

        private bool TryParseEndpoint(string target, out EndPoint endPoint)
        {
            var colonIndex = target.IndexOf(':');
            if (colonIndex == -1)
            {
                if (!int.TryParse(target, out var port))
                {
                    endPoint = null;
                    return false;
                }

                endPoint = new IPEndPoint(IPAddress.Loopback, port);
                return true;
            }
            else
            {
                var host = target.Substring(0, colonIndex);
                if (!int.TryParse(target.Substring(colonIndex + 1), out var port))
                {
                    endPoint = null;
                    return false;
                }

                endPoint = new DnsEndPoint(host, port);
                return true;
            }
        }
    }
}
