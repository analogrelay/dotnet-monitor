namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public abstract class EventPipeMessage
    {
        public abstract MessageType Type { get; }
    }
}
