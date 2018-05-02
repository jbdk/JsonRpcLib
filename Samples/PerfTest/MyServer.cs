using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLib.Server;

namespace PerfTest
{
    public class MyServer : JsonRpcServer
    {
        readonly SocketListener _listener;
        public ManualResetEventSlim ClientConnected { get; } = new ManualResetEventSlim();

        public MyServer(int port)
        {
            _listener = new SocketListener();
            _listener.Start(new IPEndPoint(IPAddress.Loopback, port));
            _listener.OnConnection(OnConnection);
        }

        private Task OnConnection(SocketConnection connection)
        {
            IClient client = AttachClient("1.2.3.4", connection);
            ClientConnected.Set();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _listener?.Stop();
            base.Dispose();
        }

        internal class StaticHandler
        {
            public static void SpeedNoArgs()
            {
            }

            public static TestResponseData TestResponse(int n = 0, string s = "")
            {
                return new TestResponseData {
                    Number = 432,
                    StringArray = new[] { "a", "b", "c", "d", "e" },
                    Text1 = "Some text"
                };
            }
        }

        internal class TestResponseData
        {
            public int Number { get; set; }
            public string Text1 { get; set; }
            public string[] StringArray { get; set; }
        }
    }
}
