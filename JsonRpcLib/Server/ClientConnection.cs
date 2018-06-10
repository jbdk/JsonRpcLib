using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;

namespace JsonRpcLib.Server
{
    public partial class JsonRpcServer
    {
        internal class ClientConnection : IClient
        {
            public delegate bool ProcessMessage(ClientConnection client, in RentedBuffer data);

            public int Id { get; }
            public bool IsConnected { get; private set; }
            public string Address { get; }

            private readonly IDuplexPipe _duplexPipe;
            private readonly ProcessMessage _process;
            private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
            private readonly AsyncLineReader _lineReader;
            private readonly Encoding _encoding;

            public ClientConnection(int id, string address, IDuplexPipe duplexPipe, ProcessMessage process, Encoding encoding)
            {
                _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
                Id = id;
                Address = address;
                _duplexPipe = duplexPipe ?? throw new ArgumentNullException(nameof(duplexPipe));
                _process = process ?? throw new ArgumentNullException(nameof(process));
                IsConnected = true;
                _lineReader = new AsyncLineReader(_duplexPipe.Input, ProcessReceivedMessage) {
                    ConnectionClosed = ConnectionClosed
                };
            }

            private void ConnectionClosed()
            {
                KillConnection();
            }

            private void ProcessReceivedMessage(in RentedBuffer buffer)
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
                    buffer.Dispose();
                }
            }

            virtual public bool WriteString(string data)
            {
                var buffer = _pool.Rent(data.Length * 2);

                try
                {
                    var bytes = _encoding.GetBytes(data, buffer);
                    buffer[bytes++] = (byte)'\n';
                    _duplexPipe.Output.Write(buffer.AsSpan(0, bytes));
                    _duplexPipe.Output.FlushAsync();
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
                var bytes = Serializer.Serialize(value);
                var len = bytes.Length;
                Span<byte> buffer = stackalloc byte[len + 1];
                bytes.AsSpan().CopyTo(buffer);
                buffer[len++] = (byte)'\n';
                _duplexPipe.Output.Write(buffer);
                _duplexPipe.Output.FlushAsync();
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

                ((IDisposable)_duplexPipe)?.Dispose();
                IsConnected = false;
                _process(this, default);
            }
        }
    }
}
