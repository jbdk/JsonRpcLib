﻿using System;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Threading.Tasks;

namespace Benchmark
{
    public class PipeServer : IDisposable
    {
        SocketListener _listener;

        public PipeServer(int port)
        {
            _listener = new SocketListener();
            _listener.Start(new IPEndPoint(IPAddress.Loopback, port));
            _listener.OnConnection(OnConnection);
        }

        private Task OnConnection(SocketConnection arg)
        {
            Task.Run(async () => {
                while (true)
                {
                    var result = await arg.Input.ReadAsync();
                    if (result.IsCompleted && result.Buffer.IsEmpty)
                        break;
                    await arg.Output.WriteAsync(result.Buffer.First);
                    await arg.Output.FlushAsync();
                    arg.Input.AdvanceTo(result.Buffer.End);
                }
            });
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _listener?.Dispose();
        }
    }
}
