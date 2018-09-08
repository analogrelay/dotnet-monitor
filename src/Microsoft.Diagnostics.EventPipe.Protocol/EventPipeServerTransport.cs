using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Transport
{
    public abstract class EventPipeServerTransport
    {
        public abstract void Listen();
        public abstract Task<IDuplexPipe> AcceptAsync(CancellationToken cancellationToken = default);
    }
}
