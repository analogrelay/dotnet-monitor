using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Transport.Protocol
{
    public class PingMessage : EventPipeMessage
    {
        public static readonly PingMessage Instance = new PingMessage();
        public override MessageType Type => MessageType.Ping;

        private PingMessage()
        {

        }
    }
}
