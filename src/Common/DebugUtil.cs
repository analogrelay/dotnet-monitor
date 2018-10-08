using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Internal.Utilities
{
    internal static class DebugUtil
    {
        [Conditional("DEBUG")]
        public static void WaitForDebuggerIfRequested(ref string[] args)
        {
            if (args.Any(a => a == "--debug"))
            {
                args = args.Where(a => a != "--debug").ToArray();
                Console.WriteLine($"Ready for debugger to attach. Process ID: {Process.GetCurrentProcess().Id}.");
                Console.WriteLine("Press ENTER to continue.");
                Console.ReadLine();
            }
        }
    }
}
