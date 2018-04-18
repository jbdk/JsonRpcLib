using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// REF: http://www.jsonrpc.org/specification

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer : IDisposable
    {
        readonly private Encoding _encoding;
        private int _nextClientId;
        private bool _disposed;
        private readonly ConcurrentDictionary<int, IClient> _clients = new ConcurrentDictionary<int, IClient>();
        public IList<IClient> Clients => _clients.Values.ToList<IClient>();
        protected Action<IClient, RentedBuffer> IncommingMessageHook { get; set; }

        /// <summary>
        /// Create a new JsorRpcServer instance
        /// </summary>
        /// <param name="encoding">The transport encoding to use (default is UTF8)</param>
        public JsonRpcServer(Encoding encoding = null)
        {
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IClient AttachClient(string address, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var id = Interlocked.Increment(ref _nextClientId);
            var client = new ClientConnection(id, address, stream, ProcessClientMessage, _encoding);

            _clients.TryAdd(id, client);
            Debug.WriteLine($"#{id} JsonRpc client added");
            return client;
        }

        private bool ProcessClientMessage(IClient client, RentedBuffer buffer)
        {
            if (buffer.IsEmpty)
            {
                HandleDisconnect(client);
                IncommingMessageHook?.Invoke(client, buffer);
                return false;
            }

//            DummyHandler(client, buffer.Span);
            //return true;

            try
            {
                var request = Serializer.Deserialize<Request>(buffer.Span);
                ExecuteHandler(client, request.Id, request.Method, request.Params);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in JsonRpcServer.ProcessClientMessage(): " + ex.Message);
            }

            IncommingMessageHook?.Invoke(client, buffer);
            return true;    // Continue receiving data from client
        }

        private void DummyHandler(ClientConnection client, ReadOnlySpan<byte> span)
        {
            var s = _encoding.GetString(span);

            int a = s.IndexOf("\"id\":") + 5;
            int b = 0;
            while (char.IsDigit(s[a + b]))
                b++;
            int id = int.Parse(s.AsSpan(a, b));
            client.WriteString($"{{\"jsonrpc\":\"2.0\",\"id\":{id}}}");
        }

        private void HandleDisconnect(IClient client)
        {
            // Client connection closed or lost
            var result = _clients.TryRemove(client.Id, out var _);
            Debug.Assert(result, "_clients.TryRemove() failed!");
            Debug.WriteLine($"#{client.Id} JsonRpc client removed");
        }

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            foreach (var client in _clients.Values)
                client.Dispose();
            _clients.Clear();

            _disposed = true;
        }
    }
}
