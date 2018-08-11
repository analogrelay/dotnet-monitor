using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public static class NetworkStreamExtensions
    {
        public static IDuplexPipe CreatePipe(this NetworkStream stream)
        {
            var appToNetwork = new Pipe();
            var networkToApp = new Pipe();

            var cts = new CancellationTokenSource();
            _ = ReceiveLoop(networkToApp, stream, cts);
            _ = SendLoop(appToNetwork, stream, cts);

            return new DuplexPipe(networkToApp.Reader, appToNetwork.Writer);
        }

        private static async Task SendLoop(Pipe pipe, NetworkStream stream, CancellationTokenSource cts)
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var result = await pipe.Reader.ReadAsync();
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;

                    try
                    {
                        // If we're cancelled, don't drain.
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            // TODO: Being lazy
                            await stream.WriteAsync(buffer.ToArray(), 0, (int)buffer.Length);
                            consumed = buffer.End;
                        }
                    }
                    finally
                    {
                        pipe.Reader.AdvanceTo(consumed);
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // No-op, we're shutting down.
            }
            finally
            {
                // Shut down the other loop if it's running.
                cts.Cancel();
                pipe.Writer.CancelPendingFlush();
            }
        }

        private static async Task ReceiveLoop(Pipe pipe, NetworkStream stream, CancellationTokenSource cts)
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    // Get a buffer from the pipe
                    var buffer = pipe.Writer.GetMemory();
                    if(!MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment))
                    {
                        throw new NotSupportedException("Only managed buffers are supported!");
                    }

                    // Read from the socket into the buffer
                    var read = await stream.ReadAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cts.Token);
                    if (read == 0)
                    {
                        break;
                    }

                    // Advance and flush the pipe
                    pipe.Writer.Advance(read);
                    var result = await pipe.Writer.FlushAsync();
                    if (result.IsCanceled || result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // No-op, we're shutting down.
            }
            catch (Exception ex)
            {
                pipe.Writer.Complete(ex);
            }
            finally
            {
                // Complete our end
                pipe.Writer.Complete();

                // Shut down the other loop
                cts.Cancel();
                pipe.Reader.CancelPendingRead();
            }
        }
    }
}
