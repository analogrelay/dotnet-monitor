using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class EventSourceCreatedMessage : EventPipeMessage
    {
        public string Name { get; }
        public Guid Guid { get; }
        public EventSourceSettings Settings { get; }

        public override MessageType Type => MessageType.EventSourceCreated;

        public EventSourceCreatedMessage(string name, Guid guid, EventSourceSettings settings)
        {
            Name = name;
            Guid = guid;
            Settings = settings;
        }
    }
}
