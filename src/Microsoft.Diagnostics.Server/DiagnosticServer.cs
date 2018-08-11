using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Server
{
    public class DiagnosticServer : IDisposable
    {
        private readonly IPEndPoint _endPoint;
        private TcpListener _listener;
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        public DiagnosticServer(IPEndPoint endPoint)
        {
            _endPoint = endPoint;
            _listener = new TcpListener(endPoint);
        }

        // TODO: Make the port unnecessary by allocating a random port and writing to a file
        // based on the process ID so that dotnet-trace can detect it.
        public static DiagnosticServer Start(IPEndPoint endPoint)
        {
            var server = new DiagnosticServer(endPoint);
            server.Start();
            return server;
        }

        public void Dispose()
        {
            _shutdownCts.Cancel();
        }

        private void Start()
        {
            _listener.Start();

            _ = AcceptLoop(_listener, _shutdownCts.Token);
        }

        private async Task AcceptLoop(TcpListener listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                Trace("Accepted socket");

                var pipe = client.GetStream().CreatePipe();
                var server = new EventPipeServer(pipe);
                _ = server.RunAsync();
            }
        }

        private static void Trace(string line)
        {
            // Cheap-n-dirty logging :)
            Console.WriteLine($"[DiagnosticServer] {line}");
        }
    }
}
