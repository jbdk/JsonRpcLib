using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly ConcurrentDictionary<int, ClientConnection> _clients = new ConcurrentDictionary<int, ClientConnection>();
        public IList<IClient> Clients => _clients.Values.ToList<IClient>();
        protected Action<IClient, string> IncommingMessageHook { get; set; }

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

        private bool ProcessClientMessage(ClientConnection client, string data)
        {
            if (data == null)
            {
                HandleDisconnect(client);
                IncommingMessageHook?.Invoke(client, data);
                return false;
            }

            //DummyHandler(client, data);

            try
            {
                var request = Serializer.Deserialize<Request>(data);
                ExecuteHandler(client, request.Id, request.Method, request.Params);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in JsonRpcServer.ProcessClientMessage(): " + ex.Message);
            }

            IncommingMessageHook?.Invoke(client, data);
            return true;    // Continue receiving data from client
        }

        private void DummyHandler(ClientConnection client, string data)
        {
            int a = data.IndexOf("\"id\":") + 5;
            int b = 0;
            while (char.IsDigit(data[a + b]))
                b++;
            int id = int.Parse(data.AsSpan(a, b));
            client.WriteString($"{{\"jsonrpc\":\"2.0\",\"id\":{id}}}");
        }

        private void HandleDisconnect(ClientConnection client)
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
