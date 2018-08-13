using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Server;

namespace SampleMonitoredApp
{
    internal class Program
    {
        private static readonly Random _rando = new Random();

        private static async Task Main(string[] args)
        {
            await DiagnosticServer.StartAsync();

            Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");
            Console.WriteLine("Ready to start emitting events. Press ENTER to emit an event, press Ctrl-C to shut down.");

            while (true)
            {
                Console.ReadLine();

                // Emit an event
                EmitEvent();
            }
        }

        private static void EmitEvent()
        {
            SampleEventSource.Log.MyEvent(_rando.Next());
        }
    }

    [EventSource(Name = "Sample-EventSource")]
    public class SampleEventSource : EventSource
    {
        public static readonly SampleEventSource Log = new SampleEventSource();

        private SampleEventSource()
        {
        }

        [Event(1, Message = "My event with payload {0}")]
        public void MyEvent(int rando)
        {
            WriteEvent(1, rando);
        }
    }
}
