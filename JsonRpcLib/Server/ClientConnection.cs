using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Utf8Json;
using Utf8Json.Resolvers;

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer
    {
        internal class ClientConnection : IClient
        {
            private const int BUFFER_SIZE = 1 * 1024 * 1024;
            private const int PACKET_SIZE = 10 * 1024;

            public int Id { get; }
            public bool IsConnected { get; private set; }
            public string Address { get; }

            private readonly Stream _stream;
            private readonly Func<ClientConnection, string, bool> _process;
            private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
            private readonly Encoding _encoding;
            private readonly Action<ClientConnection, bool> _clientIsIdle;

            private byte[] _readBuffer;
            private byte[] _packetBuffer;
            int _packetPosition;
            bool _reportedIdleStatus = false;

            public ClientConnection(int id, string address, Stream stream, Func<ClientConnection, string, bool> process, Encoding encoding)
            {
                _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
                Id = id;
                Address = address;
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _process = process ?? throw new ArgumentNullException(nameof(process));
                IsConnected = true;

                _readBuffer = _pool.Rent(BUFFER_SIZE);
                BeginRead();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void BeginRead()
            {
                _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReadCompleted2, this);
            }

            private void ReadCompleted2(IAsyncResult ar)
            {
                try
                {
                    int readCount = _stream.EndRead(ar);
                    if (readCount <= 0)
                    {
                        KillConnection();
                        return;
                    }

                    var data = _readBuffer;
                    try
                    {
                        if (_packetBuffer == null)
                        {
                            _packetBuffer = _pool.Rent(PACKET_SIZE);
                            _packetPosition = 0;
                        }
                        for (int i = 0; i < readCount; i++)
                        {
                            if (data[i] == '\n')
                            {
                                try
                                {
                                    var message = _encoding.GetString(_packetBuffer.AsSpan(0, _packetPosition));
                                    if (!_process(this, message))
                                    {
                                        KillConnection();
                                    }
                                }
                                finally
                                {
                                    _packetPosition = 0;
                                }
                            }
                            else
                            {
                                _packetBuffer[_packetPosition++] = data[i];
                            }
                        }
                    }
                    finally
                    {
                        _pool.Return(data);

                        _readBuffer = _pool.Rent(BUFFER_SIZE);
                        BeginRead();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception in ClientConnection.ReadCompleted: " + ex.Message);
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
                    // Span<byte> buffer = stackalloc byte[PACKET_SIZE];
                    JsonSerializer.Serialize(_stream, data, StandardResolver.AllowPrivateSnakeCase);
                    _stream.WriteByte((byte)'\n');
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
                if (_readBuffer != null)
                {
                    _pool.Return(_readBuffer);
                    _readBuffer = null;
                }
                if (_packetBuffer != null)
                {
                    _pool.Return(_packetBuffer);
                    _packetBuffer = null;
                }
            }

            private void KillConnection()
            {
                if (!IsConnected)
                    return;

                _stream.Close();
                IsConnected = false;
                _process(this, null);
            }
        }
    }
}
