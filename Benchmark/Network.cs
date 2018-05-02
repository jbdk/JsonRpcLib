using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines.Networking.Sockets;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace Benchmark
{
    [CoreJob]
    [MemoryDiagnoser]
    public class ResponseTime
    {
        const int PORT = 53438;

        private SimpleEchoServer _server;
        private TcpClient _client;
        private byte[] _request = Encoding.UTF8.GetBytes("Hello\n");
        private Socket _socketClient;
        private PipeServer _pipeServer;
        private SocketConnection _pipeClient;

        [GlobalSetup]
        public void Setup()
        {
            _server = new SimpleEchoServer(PORT);
            _client = new TcpClient(IPAddress.Loopback.ToString(), PORT);

            _socketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socketClient.Connect(IPAddress.Loopback, PORT);

            _pipeServer = new PipeServer(PORT + 10);
            _pipeClient = SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, PORT + 10)).Result;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _pipeClient?.Dispose();
            _pipeServer?.Dispose();
            _socketClient.Dispose();
            _client.Dispose();
            _server.Dispose();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = 100)]
        public void UsingTcpClient()
        {
            Span<byte> buffer = stackalloc byte[32];
            for (int i = 0; i < 100; i++)
            {
                _client.Client.Send(_request);
                int n = _client.Client.Receive(buffer);
                Debug.Assert(n == _request.Length);

            }
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public async Task UsingPipes()
        {
            _pipeClient.Input.CancelPendingRead();
            for (int i = 0; i < 1000; i++)
            {
                var pendingRead = _pipeClient.Input.ReadAsync();

                await _pipeClient.Output.WriteAsync(_request);
                _pipeClient.Output.FlushAsync();
                var result = await pendingRead;
                Debug.Assert(result.Buffer.Length == _request.Length);
                _pipeClient.Input.AdvanceTo(result.Buffer.End);
            }
        }

  //      [Benchmark]
        public void UsingSocketClient()
        {
            Span<byte> buffer = stackalloc byte[32];
            _socketClient.Send(_request);
            int n = _socketClient.Receive(buffer);
            Debug.Assert(n == _request.Length);
        }
    }
}
