using System;
using System.Diagnostics;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using JsonRpcLib.Client;

namespace Benchmark
{
    public class MyClient : JsonRpcClient
    {
        public MyClient(int port) : base(SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port)).Result)
        {
            Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1);
        }
    }
}
