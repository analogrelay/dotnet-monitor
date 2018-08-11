using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Client
{
    public class DiagnosticsClient
    {
        private readonly EndPoint _endPoint;
        private readonly TcpClient _client;
        private IDuplexPipe _pipe;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public event Action<EventSourceCreatedMessage> OnEventSourceCreated;
        public event Action<EventWrittenMessage> OnEventWritten;
        public DiagnosticsClient(EndPoint endPoint)
        {
            _endPoint = endPoint;
            _client = new TcpClient();
        }

        public async Task ConnectAsync()
        {
            switch (_endPoint)
            {
                case IPEndPoint ipEndPoint:
                    await _client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);
                    break;
                case DnsEndPoint dnsEndPoint:
                    await _client.ConnectAsync(dnsEndPoint.Host, dnsEndPoint.Port);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported endpoint type: {_endPoint.GetType().FullName}");
            }

            // Start processing
            _pipe = _client.GetStream().CreatePipe();

            _ = ReceiveLoop(_pipe.Input);
        }

        public async Task EnableEventsAsync(IEnumerable<EnableEventsRequest> providers)
        {
            await _writeLock.WaitAsync();
            try
            {
                EventPipeProtocol.WriteMessage(new EnableEventsMessage(providers.ToList()), _pipe.Output);
                await _pipe.Output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }
        private async Task ReceiveLoop(PipeReader reader)
        {
            try
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
                            switch (message)
                            {
                                case EventSourceCreatedMessage eventSourceCreatedMessage:
                                    _ = Task.Run(() => OnEventSourceCreated?.Invoke(eventSourceCreatedMessage));
                                    break;
                                case EventWrittenMessage eventWrittenMessage:
                                    _ = Task.Run(() => OnEventWritten?.Invoke(eventWrittenMessage));
                                    break;
                                default:
                                    throw new NotSupportedException($"Unsupported message type: {message.GetType().FullName}");
                            }
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
            catch (Exception ex)
            {
                reader.Complete(ex);
            }
            finally
            {
                reader.Complete();
            }
        }
    }
}
