using System;
using System.Diagnostics;
using System.Net.Sockets;
using JsonRpcLib.Client;

namespace PerfTest
{
    public class TestClient : JsonRpcClient
    {
        public TestClient(int port) : base(new TcpClient("127.0.0.1", port) { SendBufferSize = 32 * 1024 }.GetStream())
        {
            Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1);
        }
    }
}
