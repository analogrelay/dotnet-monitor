using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Server
{
    public class EventPipeServer
    {
        private readonly IDuplexPipe _pipe;
        private readonly EventPipeListener _listener;

        public EventPipeServer(IDuplexPipe pipe)
        {
            _pipe = pipe;
            _listener = new EventPipeListener();
        }

        public async Task RunAsync()
        {
            var cts = new CancellationTokenSource();
            var reader = ReadLoop(_pipe.Input);
            var writer = WriteLoop(_pipe.Output, cts.Token);

            var trigger = await Task.WhenAny(reader, writer);

            if (ReferenceEquals(trigger, reader))
            {
                cts.Cancel();
                await writer;
            }
            else
            {
                _pipe.Input.CancelPendingRead();
                await reader;
            }
        }

        private async Task WriteLoop(PipeWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                while (await _listener.Messages.WaitToReadAsync(cancellationToken))
                {
                    while (!cancellationToken.IsCancellationRequested && _listener.Messages.TryRead(out var message))
                    {
                        EventPipeProtocol.WriteMessage(message, writer);
                    }
                    await writer.FlushAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // No-op, we're just shutting down.
            }
        }

        private async Task ReadLoop(PipeReader reader)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    while (EventPipeProtocol.TryParseMessage(ref buffer, out var message))
                    {
                        await DispatchMessageAsync(message);
                    }
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    reader.AdvanceTo(buffer.Start);
                }
            }
        }

        private Task DispatchMessageAsync(EventPipeMessage message)
        {
            switch (message)
            {
                case EnableEventsMessage enableEventsMessage:
                    foreach (var request in enableEventsMessage.Requests)
                    {
                        _listener.EnableEvents(request);
                    }
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
