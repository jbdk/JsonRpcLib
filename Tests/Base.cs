using System.IO;
using System.IO.Pipelines;
using JsonRpcLib;
using JsonRpcLib.Server;

namespace Tests
{
    public abstract class Base
    {
        public readonly IDuplexPipe _fakePipe = new StreamDuplexPipe(PipeOptions.Default, new MemoryStream());

        public static bool Process(IClient client, in RentedBuffer data) => false;
        internal static JsonRpcServer.ClientConnection.ProcessMessage _process => Process;
    }
}
