using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace JsonRpcLib
{
    internal class AsyncLineReader : IDisposable
    {
        const int PACKET_SIZE = 10 * 1024;

        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private byte[] _packetBuffer;
        int _packetPosition;
        private readonly Stream _stream;
        private readonly Action<RentedBuffer> _processLine;

        public Action ConnectionClosed { get; set; }

        public AsyncLineReader(Stream stream, Action<RentedBuffer> processLine)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _processLine = processLine ?? throw new ArgumentNullException(nameof(processLine));

            BeginRead();
        }

        private void BeginRead()
        {
            var buffer = _pool.Rent(PACKET_SIZE);
            _stream.ReadAsync(buffer, 0, buffer.Length).ContinueWith(t => ReadCompleted(buffer, t.Result));
        }

        private void ReadCompleted(byte[] data, int readCount)
        {
            try
            {
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
