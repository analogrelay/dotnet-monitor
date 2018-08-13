using System.Collections.Generic;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class EnableEventsMessage : EventPipeMessage
    {
        public IList<EnableEventsRequest> Requests { get; }
        public override MessageType Type => MessageType.EnableEvents;

        public EnableEventsMessage(): this(new List<EnableEventsRequest>())
        {
        }

        public EnableEventsMessage(IList<EnableEventsRequest> requests)
        {
            Requests = requests;
        }
    }
}
