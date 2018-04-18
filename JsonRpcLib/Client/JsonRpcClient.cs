using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

// REF: http://www.jsonrpc.org/specification

namespace JsonRpcLib.Client
{
    public class JsonRpcClient : IDisposable
    {
        public Stream Stream { get; }
        private bool _disposed;
        private int _nextId;
        private bool _captureMode;
        private TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private readonly Encoding _encoding;
        private readonly AsyncLineReader _lineReader;
        private BlockingQueue<RentedBuffer> _responseQueue = new BlockingQueue<RentedBuffer>();

        /// <summary>
        /// Get/Set read and write timeout 
        /// </summary>
        public TimeSpan Timeout
        {
            get => _timeout;
            set {
                _timeout = value;
                Stream.ReadTimeout = Stream.WriteTimeout = (int)value.TotalMilliseconds;
            }
        }

        public JsonRpcClient(Stream baseStream, Encoding encoding = null)
        {
            Stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Stream.ReadTimeout = Stream.WriteTimeout = (int)Timeout.TotalMilliseconds;
            _encoding = encoding ?? Encoding.UTF8;
            _lineReader = new AsyncLineReader(Stream, ProcessReceivedMessage) {
                ConnectionClosed = ConnectionClosed
            };
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

            Send(request, false);
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

            var response = InvokeHelper<object>(request);   // Just ignore result object
            if (response.Error != null)
                throw new JsonRpcException(response.Error.Value);
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

            var response = InvokeHelper<T>(request);
            if (response.Error != null)
                throw new JsonRpcException(response.Error.Value);
            if (response.Id != request.Id)
                throw new JsonRpcException($"Request/response id mismatch. Expected {request.Id} but got {response.Id}");

            return response.Result;
        }

        private Response<T> InvokeHelper<T>(Request request)
        {
            Send(request);
            while(true)
            {
                var data = _responseQueue.Dequeue((int)Timeout.TotalMilliseconds);
                if (data.IsEmpty)
                    throw new TimeoutException();
                try
                {
                    var response = Serializer.Deserialize<Response<T>>(data.Span);
                    if (response.Id == request.Id)
                    {
                        return response;
                    }
                    else
                    {
                        // Some other packet received.
                        // Don't know what to do here yet !?!
                    }
                }
                finally
                {
                    data.Return();
                }
            }
        }

        public void Flush() => Stream.Flush();

        private void ConnectionClosed()
        {
        }

        private void ProcessReceivedMessage(RentedBuffer rented)
        {
            _responseQueue.Enqueue(rented);
        }

        private void Send<T>(T value, bool flush = true)
        {
            var arraySegment = JsonSerializer.SerializeUnsafe(value, Serializer.Resolver);
            var len = arraySegment.Count;
            Span<byte> buffer = stackalloc byte[len + 1];
            arraySegment.AsSpan().CopyTo(buffer);
            buffer[len++] = (byte)'\n';
            Stream.Write(buffer);
            if (flush)
                Stream.Flush();
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
            /*
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

            Stream.ReadTimeout = int.MaxValue;
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

            Stream.ReadTimeout = Timeout.Milliseconds;
            _captureMode = false;
            */
        }

        /// <summary>
        /// Dispose the base stream (cancels all pending reads). After that, the instance can not be re-used.
        /// </summary>
        public virtual void Dispose()
        {
            if (_disposed)
                return;

            Stream?.Dispose();
            _lineReader?.Dispose();
            GC.SuppressFinalize(this);

            _disposed = true;
        }
    }
}
