using System;
using System.Diagnostics;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using JsonRpcLib.Client;

namespace Tests
{
    public class TestClient : JsonRpcClient
    {
        public TestClient(int port) : base(SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port)).Result)
        {
            Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1);
        }
    }
}
