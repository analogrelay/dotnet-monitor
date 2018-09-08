using System;
using System.Net;

namespace Microsoft.Diagnostics.Transport
{
    public class TcpTransport : EventPipeTransport
    {
        private readonly IPEndPoint _endPoint;

        public TcpTransport(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public override EventPipeClientTransport CreateClient()
        {
            throw new NotImplementedException();
        }

        public override EventPipeServerTransport CreateServer()
        {
            throw new NotImplementedException();
        }
    }
}
