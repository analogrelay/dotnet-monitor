using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Client
{
    public class DiagnosticsClient
    {
        private readonly EndPoint _endPoint;
        private readonly TcpClient _client;
        private readonly Channel<EventPipeMessage> _outgoingMessages = Channel.CreateUnbounded<EventPipeMessage>();
        private readonly Channel<EventPipeMessage> _incomingMessages = Channel.CreateUnbounded<EventPipeMessage>();

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
            var pipe = _client.GetStream().CreatePipe();

            _ = RunClientAsync(pipe);
        }

        public ValueTask<EventPipeMessage> ReceiveAsync(CancellationToken cancellationToken = default) => _incomingMessages.Reader.ReadAsync(cancellationToken);
        public ValueTask SendAsync(EventPipeMessage message, CancellationToken cancellationToken = default) => _outgoingMessages.Writer.WriteAsync(message, cancellationToken);

        private async Task RunClientAsync(IDuplexPipe pipe)
        {
            var receiver = ReceiveLoop(pipe.Input);
            var sender = SendLoop(pipe.Output, _outgoingMessages.Reader);

            var trigger = await Task.WhenAny(receiver, sender);

            if (ReferenceEquals(trigger, receiver))
            {
                _outgoingMessages.Writer.TryComplete();
                await sender;
            }
            else
            {
                pipe.Input.CancelPendingRead();
                await receiver;
            }
        }

        private async Task SendLoop(PipeWriter output, ChannelReader<EventPipeMessage> messages)
        {
            try
            {
                while (await messages.WaitToReadAsync())
                {
                    while (messages.TryRead(out var message))
                    {
                        throw new NotImplementedException("Can't send messages yet!");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // No-op
            }
            catch (Exception ex)
            {
                output.Complete(ex);
            }
            finally
            {
                output.Complete();
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
                            await _incomingMessages.Writer.WriteAsync(message);
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
