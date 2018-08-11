namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class EventWrittenMessage : EventPipeMessage
    {
        public override MessageType Type => MessageType.EventWritten;

        public string ProviderName { get; }
        public int EventId { get; }
        public string EventName { get; }

        public EventWrittenMessage(string providerName, int eventId, string eventName)
        {
            ProviderName = providerName;
            EventId = eventId;
            EventName = eventName;
        }
    }
}
