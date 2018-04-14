using System;
using System.Diagnostics;
using System.Net.Sockets;
using JsonRpcLib.Client;

namespace Tests
{
    public class TestClient : JsonRpcClient
    {
        public TestClient(int port) : base(new TcpClient("127.0.0.1", port).GetStream())
        {
            Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1);
        }

        internal int ReceiveDataWithin(int timeoutInMs)
        {
            var t = Stream.ReadAsync(new byte[256], 0, 256);
            if (t.Wait(timeoutInMs))
                return t.Result;
            return 0;
        }
    }
}
