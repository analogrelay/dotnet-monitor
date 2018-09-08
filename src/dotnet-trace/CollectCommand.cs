using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Diagnostics.Transport.Protocol;

namespace Microsoft.Diagnostics.Tools.Trace
{
    [Command(Name = Name, Description = "Collects events from the target process")]
    public class CollectCommand : TargetCommandBase
    {
        public const string Name = "collect";

        [Argument(0, "<PROVIDERS>", "The providers to collect from.")]
        public IList<string> Providers { get; }

        [Option("--mel", Description = "Set this switch to collect from Microsoft.Extensions.Logging")]
        public bool CollectMicrosoftExtensionsLogging { get; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
            var disconnectCts = new CancellationTokenSource();
            var cancellationToken = console.GetCtrlCToken();

            if (!TryCreateClient(console, out var client))
            {
                return 1;
            }

            console.WriteLine("Connecting to application...");

            client.OnEventWritten += (evt) =>
            {
                // TODO: Format both kinds of messages ("Foo {0}" and "Foo {foo}")
                console.WriteLine($"{evt.ProviderName}/{evt.EventName}({evt.EventId}): {evt.Message}");
                for (var i = 0; i < evt.Payload.Count; i++)
                {
                    console.WriteLine($"  {evt.PayloadNames[i]}: {evt.Payload[i]}");
                }
            };

            client.Disconnected += (ex) =>
            {
                console.WriteLine($"Disconnected: {ex}");
                disconnectCts.Cancel();
            };

            await client.ConnectAsync();

            if (CollectMicrosoftExtensionsLogging)
            {
                await client.EnableMicrosoftExtensionsLoggingAsync();
            }

            if (Providers != null && Providers.Any())
            {
                await client.EnableEventsAsync(Providers.Select(p => new EnableEventsRequest(p, EventLevel.Verbose, EventKeywords.All)));
            }

            console.WriteLine("Connected, press Ctrl-C to terminate...");
            await CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disconnectCts.Token).WaitForCancellationAsync();

            return 0;
        }
    }
}
