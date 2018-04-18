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
            private readonly Func<ClientConnection, string, bool> _process;
            private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
            private readonly AsyncLineReader _lineReader;
            private readonly Encoding _encoding;

            public ClientConnection(int id, string address, Stream stream, Func<ClientConnection, string, bool> process, Encoding encoding)
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
                    var message = _encoding.GetString(buffer.Span);
                    if (!_process(this, message))
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
                try
                {
                    var buffer = _pool.Rent(data.Length * 2);
                    var bytes = _encoding.GetBytes(data, buffer);
                    buffer[bytes++] = (byte)'\n';
                    _stream.BeginWrite(buffer, 0, bytes, (ar) => _pool.Return((byte[])ar.AsyncState), buffer);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in ClientConnection.Write(): " + ex.Message);
                    KillConnection();
                    return false;
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
                //_stream.Flush();
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
                _process(this, null);
            }
        }
    }
}
