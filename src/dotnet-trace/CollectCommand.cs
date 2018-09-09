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

        [Option("-p|--provider <PROVIDER>", "An EventSource provider to enable.", CommandOptionType.MultipleValue)]
        public IList<string> Providers { get; }

        [Option("-l|--logger <LOGGER>", "A Microsoft.Extensions.Logging logger prefix to enable.", CommandOptionType.MultipleValue)]
        public IList<string> Loggers { get; }

        [Option("-c|--counter <LOGGER>", "An EventSource to enable counters for.", CommandOptionType.MultipleValue)]
        public IList<string> Counters { get; }

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

            client.OnEventCounterUpdated += (state) =>
            {
                console.WriteLine($"Counter: {state.ProviderName}/{state.CounterName} (Avg: {state.Mean}, StdDev: {state.StandardDeviation}, Count: {state.Count}, Min: {state.Min}, Max: {state.Max})");
            };

            client.Disconnected += (ex) =>
            {
                console.WriteLine("Disconnected");
                if (ex != null)
                {
                    console.Error.WriteLine(ex.ToString());
                }
                disconnectCts.Cancel();
            };

            await client.ConnectAsync();

            var enabledSomething = false;
            if (Loggers != null && Loggers.Any())
            {
                await client.EnableLoggersAsync(Loggers);
                enabledSomething = true;
            }

            if (Counters != null && Counters.Any())
            {
                await client.EnableCountersAsync(Counters);
                enabledSomething = true;
            }

            if (Providers != null && Providers.Any())
            {
                await client.EnableEventsAsync(Providers.Select(p => new EnableEventsRequest(p, EventLevel.Verbose, EventKeywords.All)));
                enabledSomething = true;
            }

            if (!enabledSomething)
            {
                console.Error.WriteLine("At least one of '--provider', '--logger', or '--counter' must be provided.");
                return 1;
            }

            console.WriteLine("Connected, press Ctrl-C to terminate...");
            await CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disconnectCts.Token).WaitForCancellationAsync();

            return 0;
        }
    }
}
