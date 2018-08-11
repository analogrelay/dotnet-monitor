using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class EnableEventsRequest
    {
        public string Provider { get; }
        public EventLevel Level { get; }
        public EventKeywords Keywords { get; }

        public EnableEventsRequest(string provider, EventLevel level, EventKeywords keywords)
        {
            Provider = provider;
            Level = level;
            Keywords = keywords;
        }
    }
}
