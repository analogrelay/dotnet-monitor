using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Channels;
using Microsoft.Diagnostics.EventPipe.Protocol;

namespace Microsoft.Diagnostics.Server
{
    /// <summary>
    /// Listens to the creation of event sources and events and produces <see cref="EventPipeMessage"/> objects representing them.
    /// </summary>
    public class EventPipeListener : EventListener
    {
        // Things created in field initializer DO run before OnEventSourceCreated
        // This also means we can't make the Channel settings here configurable :(
        private readonly Channel<EventPipeMessage> _messages = Channel.CreateUnbounded<EventPipeMessage>();

        public ChannelReader<EventPipeMessage> Messages => _messages.Reader;

        public EventPipeListener()
        {
            // WARNING: Any code here is going to run AFTER the OnEventSourceCreated method is fired for
            // event sources that existed prior to the listener being constructed
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            var message = new EventSourceCreatedMessage(eventSource.Name, eventSource.Guid, eventSource.Settings);
            var successful = _messages.Writer.TryWrite(message);
            Debug.Assert(successful, "Channel should be unbounded!");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
        }
    }
}
