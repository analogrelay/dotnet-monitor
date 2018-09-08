using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Transport.Protocol
{
    public class EnableEventsRequest
    {
        public string Provider { get; set; }
        public EventLevel Level { get; set; }
        public EventKeywords Keywords { get; set; }

        public EnableEventsRequest()
        {
        }

        public EnableEventsRequest(string provider, EventLevel level, EventKeywords keywords) : this()
        {
            Provider = provider;
            Level = level;
            Keywords = keywords;
        }
    }
}
