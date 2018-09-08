namespace Microsoft.Diagnostics.Transport.Protocol
{
    public abstract class EventPipeMessage
    {
        public abstract MessageType Type { get; }
    }
}
