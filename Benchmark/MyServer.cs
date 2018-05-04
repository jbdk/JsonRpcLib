using System;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLib.Server;

namespace Benchmark
{
    public class MyServer : JsonRpcServer
    {
        readonly SocketListener _listener;
        public ManualResetEventSlim ClientConnected { get; } = new ManualResetEventSlim();
        TaskCompletionSource<int> _tcs = new TaskCompletionSource<int>();
        public string LastMessageReceived { get; private set; }

        public MyServer(int port)
        {
            IncommingMessageHook = (_, mem) => LastMessageReceived = Encoding.UTF8.GetString(mem.Span);

            _listener = new SocketListener();
            _listener.Start(new IPEndPoint(IPAddress.Loopback, port));
            _listener.OnConnection(OnConnection);

            Bind(typeof(StaticHandler));
        }

        private Task OnConnection(SocketConnection connection)
        {
            IClient client = AttachClient("1.2.3.4", connection);
            ClientConnected.Set();
            return _tcs.Task;
        }

        public override void Dispose()
        {
            _listener?.Stop();
            base.Dispose();
        }

        internal static class StaticHandler
        {
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
