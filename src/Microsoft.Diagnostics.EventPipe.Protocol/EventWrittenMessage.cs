using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class EventWrittenMessage : EventPipeMessage
    {
        public override MessageType Type => MessageType.EventWritten;

        public string ProviderName { get; }
        public int EventId { get; }
        public string EventName { get; }
        public EventKeywords Keywords { get; }
        public EventLevel Level { get; }
        public string Message { get; }
        public EventOpcode Opcode { get; }
        public Guid RelatedActivityId { get; }
        public EventTags Tags { get; }
        public EventTask Task { get; }
        public byte Version { get; }
        public Guid ActivityId { get; }
        public EventChannel Channel { get; }
        public IDictionary<string, object> Payload { get; } = new Dictionary<string, object>();

        [JsonConstructor]
        private EventWrittenMessage()
        {
        }

        public EventWrittenMessage(EventWrittenEventArgs args)
        {
            ActivityId = args.ActivityId;
            Channel = args.Channel;
            EventId = args.EventId;
            EventName = args.EventName;
            ProviderName = args.EventSource.Name;
            Keywords = args.Keywords;
            Level = args.Level;
            Message = args.Message;
            Opcode = args.Opcode;
            RelatedActivityId = args.RelatedActivityId;
            Tags = args.Tags;
            Task = args.Task;
            Version = args.Version;

            for (var i = 0; i < args.PayloadNames.Count; i++)
            {
                Payload[args.PayloadNames[i]] = args.Payload[i];
            }
        }
    }
}
