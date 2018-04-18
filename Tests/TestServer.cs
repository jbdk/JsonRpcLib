﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JsonRpcLib.Server;

namespace Tests
{
    public class TestServer : JsonRpcServer
    {
        readonly TcpListener _listener;
        public ManualResetEventSlim ClientConnected { get; } = new ManualResetEventSlim();
        public string LastMessageReceived { get; private set; }

        public TestServer(int port)
        {
            IncommingMessageHook = (_, mem) => LastMessageReceived = Encoding.UTF8.GetString(mem.Span);

            Bind<StaticHandler>();

            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            _listener.BeginAcceptTcpClient(AcceptCallback, this);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                var tcpClient = _listener.EndAcceptTcpClient(ar);
                tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                IClient client = AttachClient("1.2.3.4", tcpClient.GetStream());
                ClientConnected.Set();
                _listener.BeginAcceptTcpClient(AcceptCallback, this);
            }
            catch (ObjectDisposedException)
            {
                // NOP (we closed the socket)
            }
        }

        public override void Dispose()
        {
            _listener?.Stop();
            base.Dispose();
        }

        internal class StaticHandler
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
