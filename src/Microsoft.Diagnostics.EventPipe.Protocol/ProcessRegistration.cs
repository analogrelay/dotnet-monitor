namespace Microsoft.Diagnostics.EventPipe.Protocol
{
    public class ProcessRegistration
    {
        public int ProcessId { get; }
        public int DiagnosticsPort { get; }
        public string ImageName { get; }
        public string CommandLine { get; }
        public int ProtocolVersion { get; }

        public ProcessRegistration(int processId, int diagnosticsPort, string imageName, string commandLine, int protocolVersion)
        {
            ProcessId = processId;
            DiagnosticsPort = diagnosticsPort;
            ImageName = imageName;
            CommandLine = commandLine;
            ProtocolVersion = protocolVersion;
        }
    }
}
