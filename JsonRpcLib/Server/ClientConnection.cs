using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

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

            private void ProcessReceivedMessage(Memory<byte> data)
            {
                var message = _encoding.GetString(data.Span);
                if (!_process(this, message))
                {
                    KillConnection();
                }
            }

            virtual public bool Write(string data)
            {
                try
                {
                    Span<byte> buffer = stackalloc byte[data.Length * 2];
                    var bytes = _encoding.GetBytes(data, buffer);
                    buffer[bytes] = (byte)'\n';
                    _stream.Write(buffer.Slice(0, bytes + 1));
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in ClientConnection.Write(): " + ex.Message);
                    KillConnection();
                    return false;
                }
            }

            virtual public void WriteAsJson(object data)
            {
                try
                {
                    var json = Utf8Json.JsonSerializer.ToJsonString(data, Serializer.Resolver);
                    Span<byte> buffer = stackalloc byte[json.Length * 2];
                    int len = _encoding.GetBytes(json, buffer);
                    buffer[len] = (byte)'\n';
                    _stream.Write(buffer.Slice(0, len + 1));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in ClientConnection.Write(): " + ex.Message);
                    KillConnection();
                }
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
