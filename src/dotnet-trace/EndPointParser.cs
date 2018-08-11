using System.Net;

namespace Microsoft.Diagnostics.Tools.Trace
{
    internal static class EndPointParser
    {
        public static bool TryParseEndpoint(string target, out EndPoint endPoint)
        {
            var colonIndex = target.IndexOf(':');
            if (colonIndex == -1)
            {
                if (!int.TryParse(target, out var port))
                {
                    endPoint = null;
                    return false;
                }

                endPoint = new IPEndPoint(IPAddress.Loopback, port);
                return true;
            }
            else
            {
                var host = target.Substring(0, colonIndex);
                if (!int.TryParse(target.Substring(colonIndex + 1), out var port))
                {
                    endPoint = null;
                    return false;
                }

                endPoint = new DnsEndPoint(host, port);
                return true;
            }
        }
    }
}
