using System;
using System.Diagnostics;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JsonRpcLib.Client;

namespace PerfTest
{
    public class MyClient
    {
        private readonly SocketConnection _conn;

        public static async Task<JsonRpcClient> ConnectAsync(int port)
        {
            var c = await SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
            return new JsonRpcClient(c) {
                Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1)
            };
        }
    }
}

