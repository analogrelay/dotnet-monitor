using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    // Manages the list of processes that have diagnostic servers running
    public static class ProcessLocator
    {
        private static readonly string BaseDirectory = ComputeBaseDirectory();
        private const string FileSuffix = ".process.json";
        private static readonly int FileSuffixLength = FileSuffix.Length;

        /// <summary>
        /// Registers the specified information in the list of diagostic processes. Returns a disposable that will
        /// clean up the registration when disposed.
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static async Task<IDisposable> RegisterProcessAsync(ProcessRegistration process)
        {
            if (BaseDirectory == null)
            {
                // No-op.
                return null;
            }

            var pidFile = Path.Combine(BaseDirectory, $"{process.ProcessId}{FileSuffix}");

            if (!Directory.Exists(BaseDirectory))
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            using (var stream = new FileStream(pidFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize: 4096))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    var json = JsonConvert.SerializeObject(process);
                    await writer.WriteAsync(json);
                }
            }

            return null;
        }

        public static Task<ProcessRegistration> GetRegistrationAsync(int processId)
        {
            try
            {
                var content = File.ReadAllText(Path.Combine(BaseDirectory, $"{processId}{FileSuffix}"));
                return Task.FromResult(JsonConvert.DeserializeObject<ProcessRegistration>(content));
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult<ProcessRegistration>(null);
            }
        }

        public static Task<IReadOnlyList<int>> GetAllProcessIdsAsync()
        {
            if (!Directory.Exists(BaseDirectory))
            {
                return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
            }

            var list = new List<int>();
            foreach (var file in Directory.EnumerateFiles(BaseDirectory))
            {
                var name = Path.GetFileName(file);
                if (name.EndsWith(FileSuffix) && int.TryParse(name.Substring(0, name.Length - FileSuffixLength), out var pid))
                {
                    list.Add(pid);
                }
            }

            return Task.FromResult<IReadOnlyList<int>>(list);
        }

        private static string ComputeBaseDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet", "diagnostics", "processes");
            }
            else
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(home))
                {
                    // No place to store the list :(
                    return null;
                }
                else
                {
                    return Path.Combine(home, ".dotnet", "diagnostics", "processes");
                }
            }
        }
    }
}
