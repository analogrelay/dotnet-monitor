using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Transport
{
    public abstract class EventPipeClientTransport
    {
        public abstract Task<IDuplexPipe> ConnectAsync(CancellationToken cancellationToken = default);
    }
}
