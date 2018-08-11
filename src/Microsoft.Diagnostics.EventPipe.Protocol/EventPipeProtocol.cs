using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public static class EventPipeProtocol
    {
        // I got lazy and just did JSON. We'll figure it out later.

        private const byte RecordSeparator = 0x1E;
        private static readonly ReadOnlyMemory<byte> RecordSeparatorMemory = new byte[] { RecordSeparator };

        public static bool TryParseMessage(ref ReadOnlySequence<byte> input, out EventPipeMessage message)
        {
            if (input.PositionOf(RecordSeparator) is SequencePosition position)
            {
                var buffer = input.Slice(input.Start, position);
                input = input.Slice(input.GetPosition(1, position));

                message = ParseMessage(buffer);
                return true;
            }
            else
            {
                message = null;
                return false;
            }
        }

        public static void WriteMessage(EventPipeMessage message, PipeWriter writer)
        {
            JObject json;
            switch (message)
            {
                case EventSourceCreatedMessage eventSourceCreatedMessage:
                    json = new JObject(
                        new JProperty("type", MessageType.EventSourceCreated),
                        new JProperty("payload", JObject.FromObject(eventSourceCreatedMessage)));
                    break;
                default:
                    throw new NotSupportedException($"Unknown message type: {message.GetType().FullName}");
            }

            var bytes = Encoding.UTF8.GetBytes(json.ToString(Formatting.None));
            writer.Write(bytes.AsSpan());
            writer.Write(RecordSeparatorMemory.Span);
        }

        private static EventPipeMessage ParseMessage(ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                return ParseMessage(input.First.Span);
            }
            else
            {
                return ParseMessage(input.ToArray());
            }
        }

        private static EventPipeMessage ParseMessage(ReadOnlySpan<byte> input)
        {
            string str;
            unsafe
            {
                fixed (byte* i = input)
                {
                    str = Encoding.UTF8.GetString(i, input.Length);
                }
            }

            JObject json = JObject.Parse(str);
            var type = (MessageType)(int)json["type"];

            switch (type)
            {
                case MessageType.EventSourceCreated: return json["payload"].ToObject<EventSourceCreatedMessage>();
                default: throw new NotSupportedException($"Unknown message type: {type}");
            }
        }
    }
}
