using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace JsonRpcLib
{
    internal class AsyncLineReader : IDisposable
    {
        private const int BUFFER_SIZE = 1 * 1024 * 1024;
        const int PACKET_SIZE = 10 * 1024;

        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private byte[] _packetBuffer;
        int _packetPosition;
        private readonly Stream _stream;
        private readonly Action<RentedBuffer> _processLine;
        private object _lock = new object();

        public Action ConnectionClosed { get; set; }

        public AsyncLineReader(Stream stream, Action<RentedBuffer> processLine)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _processLine = processLine ?? throw new ArgumentNullException(nameof(processLine));
            BeginRead();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead()
        {
            var buffer = _pool.Rent(PACKET_SIZE);
            _stream.BeginRead(buffer, 0, buffer.Length, ReadCompleted, buffer);
        }

        private void ReadCompleted(IAsyncResult ar)
        {
            var data = (byte[])ar.AsyncState;

            try
            {
                int readCount = _stream.EndRead(ar);
                if (readCount <= 0)
                {
                    ConnectionClosed?.Invoke();
                    return;
                }

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
                            int size = _packetPosition;
                            var p = _packetBuffer;
                            _packetBuffer = _pool.Rent(PACKET_SIZE);
                            _packetPosition = 0;

                            _processLine(new RentedBuffer(p, size, (mem) => _pool.Return(mem)));
                        }
                        else
                        {
                            _packetBuffer[_packetPosition++] = data[i];
                        }
                    }
                }
                finally
                {
                    BeginRead();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in AsyncStreamReader.ReadCompleted: " + ex.Message);
                ConnectionClosed?.Invoke();
            }
            finally
            {
                _pool.Return(data);
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
