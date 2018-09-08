using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Transport;
using Microsoft.Diagnostics.Transport.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.Diagnostics.Client
{
    public class DiagnosticsClient
    {
        private static readonly EventLevel[] _mappingArray = new EventLevel[]
        {
            EventLevel.Verbose,
            EventLevel.Verbose,
            EventLevel.Informational,
            EventLevel.Warning,
            EventLevel.Error,
            EventLevel.Critical,
        };

        private IDuplexPipe _pipe;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly EventPipeClientTransport _transport;

        public event Action<EventSourceCreatedMessage> OnEventSourceCreated;
        public event Action<EventWrittenMessage> OnEventWritten;
        public event Action<Exception> Disconnected;

        public DiagnosticsClient(Uri uri)
        {
            _transport = EventPipeTransport.Create(uri).CreateClient();
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Connect transport
            _pipe = await _transport.ConnectAsync(cancellationToken);

            // Start receive loop.
            _ = ReceiveLoop(_pipe.Input);
        }

        public async Task EnableMicrosoftExtensionsLoggingAsync()
        {
            await _writeLock.WaitAsync();
            try
            {
                EventPipeProtocol.WriteMessage(
                    new EnableEventsMessage(new[] {
                        new EnableEventsRequest("Microsoft-Extensions-Logging", EventLevel.Verbose, (EventKeywords)2),
                    }),
                    _pipe.Output);
                await _pipe.Output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public async Task EnableEventsAsync(IEnumerable<EnableEventsRequest> requests)
        {
            await _writeLock.WaitAsync();
            try
            {
                EventPipeProtocol.WriteMessage(new EnableEventsMessage(requests.ToList()), _pipe.Output);
                await _pipe.Output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }
        private async Task ReceiveLoop(PipeReader reader)
        {
            Exception shutdownEx = null;
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
                            return;
                        }

                        while (EventPipeProtocol.TryParseMessage(ref buffer, out var message))
                        {
                            switch (message)
                            {
                                case EventSourceCreatedMessage eventSourceCreatedMessage:
                                    _ = Task.Run(() => OnEventSourceCreated?.Invoke(eventSourceCreatedMessage));
                                    break;
                                case EventWrittenMessage eventWrittenMessage:
                                    if (eventWrittenMessage.ProviderName.Equals("Microsoft-Extensions-Logging") && eventWrittenMessage.EventId == 2)
                                    {
                                        eventWrittenMessage = ProcessMelMessage(eventWrittenMessage);
                                    }
                                    _ = Task.Run(() => OnEventWritten?.Invoke(eventWrittenMessage));
                                    break;
                                default:
                                    throw new NotSupportedException($"Unsupported message type: {message.GetType().FullName}");
                            }
                        }

                        if (result.IsCompleted)
                        {
                            return;
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
                shutdownEx = ex;
            }
            finally
            {
                _ = Task.Run(() => Disconnected?.Invoke(shutdownEx));
                reader.Complete();
            }
        }

        private EventWrittenMessage ProcessMelMessage(EventWrittenMessage inputMessage)
        {
            var payloadDict = Enumerable.Range(0, inputMessage.Payload.Count).ToDictionary(
                i => inputMessage.PayloadNames[i],
                i => inputMessage.Payload[i]);

            var args = (JArray)payloadDict["Arguments"];

            var outputMessage = new EventWrittenMessage()
            {
                EventName = (string)payloadDict["EventId"],
                Level = MapLogLevel((long)payloadDict["Level"]),
                ProviderName = (string)payloadDict["LoggerName"],
                ActivityId = inputMessage.ActivityId,
                Channel = inputMessage.Channel,
                Version = inputMessage.Version,
                EventId = inputMessage.EventId,
                Keywords = inputMessage.Keywords,
                Opcode = inputMessage.Opcode,
                RelatedActivityId = inputMessage.RelatedActivityId,
                Tags = inputMessage.Tags,
                Task = inputMessage.Task,
            };

            foreach (var arg in args)
            {
                var obj = (JObject)arg;
                var key = obj.Value<string>("Key");
                var value = obj.Value<string>("Value");
                if (key.Equals("{OriginalFormat}"))
                {
                    outputMessage.Message = value;
                }
                else
                {
                    outputMessage.PayloadNames.Add(key);
                    outputMessage.Payload.Add(value);
                }
            }

            return outputMessage;
        }

        private EventLevel MapLogLevel(long inputLogLevel)
        {
            if (inputLogLevel < 0 || inputLogLevel > _mappingArray.Length)
            {
                return EventLevel.LogAlways;
            }
            return _mappingArray[inputLogLevel];
        }
    }
}
