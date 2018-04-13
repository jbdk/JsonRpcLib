using System;
using System.IO;
using System.Text;
using System.Threading;

// REF: http://www.jsonrpc.org/specification

namespace JsonRpcLib.Client
{
    public class JsonRpcClient : IDisposable
    {
        private readonly Stream _baseStream;
        private bool _disposed;
        private int _nextId;
        private bool _captureMode;
        private TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly Encoding _encoding;
        protected StreamReader Reader { get; }

        /// <summary>
        /// Get/Set read and write timeout 
        /// </summary>
        public TimeSpan Timeout
        {
            get => _timeout;
            set {
                _timeout = value;
                _baseStream.ReadTimeout = _baseStream.WriteTimeout = (int)value.TotalMilliseconds;
            }
        }

        public JsonRpcClient(Stream baseStream, Encoding encoding = null)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _baseStream.ReadTimeout = _baseStream.WriteTimeout = (int)Timeout.TotalMilliseconds;
            _encoding = encoding ?? Encoding.UTF8;
            Reader = new StreamReader(_baseStream, _encoding);
        }

        public void Notify(string method, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Can not be null or empty", nameof(method));

            var request = new Notify {
                Method = method,
                Params = args.Length == 0 ? null : args
            };

            string json = Serializer.Serialize(request);
            WriteLine(json);
        }

        public void Invoke(string method)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Can not be null or empty", nameof(method));

            var request = new Request {
                Id = Interlocked.Increment(ref _nextId),
                Method = method,
            };

            string json = Serializer.Serialize(request);
            Reader.DiscardBufferedData();
            WriteLine(json);

            json = Reader.ReadLine();
            var response = Serializer.Deserialize<Response>(json);
            if (response.Error != null)
                throw new JsonRpcException(response.Error);
            if (response.Id != request.Id)
                throw new JsonRpcException($"Request/response id mismatch. Expected {request.Id} but got {response.Id}");
        }

        public T Invoke<T>(string method, params object[] args)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Can not be null or empty", nameof(method));

            var request = new Request {
                Id = Interlocked.Increment(ref _nextId),
                Method = method,
                Params = args.Length == 0 ? null : args
            };

            string json = Serializer.Serialize(request);
            Reader.DiscardBufferedData();
            WriteLine(json);

            json = Reader.ReadLine();
            var response = Serializer.Deserialize<Response<T>>(json);
            if (response.Error != null)
                throw new JsonRpcException(response.Error);
            if (response.Id != request.Id)
                throw new JsonRpcException($"Request/response id mismatch. Expected {request.Id} but got {response.Id}");

            return response.Result;
        }

        private void WriteLine(string json)
        {
            var bytes = _encoding.GetBytes(json + "\n");
            _baseStream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Captures the raw json messages received from the server for the rest of the connection lifetime.
        /// Calls the handler with each message for you to parse. These messages might not conform to json RPC, 
        /// and are server specific!
        /// Return false from the handler to stop processing messages
        /// </summary>
        /// <param name="initiateMethod">The method used to initiate this on the server. (sent as a invoke request)</param>
        /// <param name="handler">The callback handler</param>
        public void EnterCaptureMode(string initiateMethod, Func<string, bool> handler)
        {
            if (string.IsNullOrWhiteSpace(initiateMethod))
                throw new ArgumentException("Can not be null or empty", nameof(initiateMethod));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (_captureMode)
                throw new JsonRpcException("Already in capture mode!");
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));

            _captureMode = true;

            // Tell the server to start doing its stuff
            Invoke(initiateMethod);

            _baseStream.ReadTimeout = int.MaxValue;
            try
            {
                while (true)
                {
                    var s = Reader.ReadLine();
                    if (string.IsNullOrEmpty(s))
                        break;
                    if (!handler(s))
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                // NOP
            }

            _baseStream.ReadTimeout = Timeout.Milliseconds;
            _captureMode = false;
        }

        /// <summary>
        /// Dispose the base stream (cancels all pending reads). After that, the instance can not be re-used.
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _baseStream?.Dispose();
            GC.SuppressFinalize(this);

            _disposed = true;
        }
    }
}
