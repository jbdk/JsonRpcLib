using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLib.Server;

namespace Benchmark
{
    public class SimpleEchoServer : IDisposable
    {
        readonly TcpListener _listener;

        public SimpleEchoServer(int port)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(AcceptCallback, this);
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var tcpClient = _listener.EndAcceptTcpClient(ar);
            Task.Run(() => {
                int n = 0;
                Span<byte> buffer = stackalloc byte[32];
                while (true)
                {
                    n = tcpClient.Client.Receive(buffer);
                    if (n <= 0)
                        break;
                    tcpClient.Client.Send(buffer.Slice(0, n));
                }
            });
            _listener.BeginAcceptTcpClient(AcceptCallback, this);
        }
    }
}
