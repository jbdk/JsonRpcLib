using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using Utf8Json;

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer
    {
        internal class ClientConnection : IClient
        {
            public int Id { get; }
            public bool IsConnected { get; private set; }
            public string Address { get; }

            private readonly Stream _stream;
            private readonly Func<ClientConnection, RentedBuffer, bool> _process;
            private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
            private readonly AsyncLineReader _lineReader;
            private readonly Encoding _encoding;

            public ClientConnection(int id, string address, Stream stream, Func<ClientConnection, RentedBuffer, bool> process, Encoding encoding)
            {
                _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
                Id = id;
                Address = address;
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _process = process ?? throw new ArgumentNullException(nameof(process));
                IsConnected = true;
                _lineReader = new AsyncLineReader(_stream, ProcessReceivedMessage) {
                    ConnectionClosed = ConnectionClosed
                };
            }

            private void ConnectionClosed()
            {
                KillConnection();
            }

            private void ProcessReceivedMessage(RentedBuffer buffer)
            {
                try
                {
                    if (!_process(this, buffer))
                    {
                        KillConnection();
                    }
                }
                finally
                {
                    buffer.Return();
                }
            }

            virtual public bool WriteString(string data)
            {
                var buffer = _pool.Rent(data.Length * 2);

                try
                {
                    var bytes = _encoding.GetBytes(data, buffer);
                    buffer[bytes++] = (byte)'\n';
                    _stream.Write(buffer, 0, bytes);
                    _stream.Flush();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in ClientConnection.Write(): " + ex.Message);
                    KillConnection();
                    return false;
                }
                finally
                {
                    _pool.Return(buffer);
                }
            }

            virtual public void WriteAsJson(object value)
            {
                var arraySegment = JsonSerializer.SerializeUnsafe(value, Serializer.Resolver);
                var len = arraySegment.Count;
                Span<byte> buffer = stackalloc byte[len + 1];
                arraySegment.AsSpan().CopyTo(buffer);
                buffer[len++] = (byte)'\n';
                _stream.Write(buffer);
                _stream.Flush();
            }

            public void Dispose()
            {
                KillConnection();
                _lineReader.Dispose();
            }

            private void KillConnection()
            {
                if (!IsConnected)
                    return;

                _stream.Dispose();
                IsConnected = false;
                _process(this, default);
            }
        }
    }
}
