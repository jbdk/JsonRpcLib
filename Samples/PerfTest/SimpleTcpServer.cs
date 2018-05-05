using System;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PerfTest
{
    public class SimpleTcpServer : IDisposable
    {
        readonly SocketListener _listener;
        readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public SimpleTcpServer(int port)
        {
            _listener = new SocketListener();
            _listener.Start(new IPEndPoint(IPAddress.Loopback, port));
            _listener.OnConnection(OnConnection);
        }

        private async Task OnConnection(SocketConnection connection)
        {
            while(!_cts.IsCancellationRequested)
            {
                var result = await connection.Input.ReadAsync(_cts.Token);
                if (result.IsCompleted && result.Buffer.IsEmpty)
                    break;

                foreach(var buf in result.Buffer)
                {
                    await connection.Output.WriteAsync(buf);
                    await connection.Output.FlushAsync();
                }

                connection.Input.AdvanceTo(result.Buffer.End);
            }
        }

        public void Dispose()
        {
            _cts.Dispose();
            _listener?.Stop();
        }
    }
}
