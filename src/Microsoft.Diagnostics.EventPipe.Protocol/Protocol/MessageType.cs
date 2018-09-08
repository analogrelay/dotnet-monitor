namespace Microsoft.Diagnostics.Transport.Protocol
{
    public enum MessageType
    {
        Ping = 1,
        EventSourceCreated = 2,
        EnableEvents = 3,
        EventWritten = 4,
    }
}
