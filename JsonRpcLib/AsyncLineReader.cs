using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace JsonRpcLib
{
    internal class AsyncLineReader : IDisposable
    {
        private const int BUFFER_SIZE = 1 * 1024 * 1024;
        const int PACKET_SIZE = 10 * 1024;

        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private byte[] _readBuffer;
        private byte[] _packetBuffer;
        int _packetPosition;
        private readonly Stream _stream;
        private readonly Action<Memory<byte>> _processLine;

        public Action ConnectionClosed { get; set; }

        public AsyncLineReader(Stream stream, Action<Memory<byte>> processLine)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _processLine = processLine ?? throw new ArgumentNullException(nameof(processLine));

            _readBuffer = _pool.Rent(BUFFER_SIZE);
            BeginRead();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BeginRead() => _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReadCompleted, this);

        private void ReadCompleted(IAsyncResult ar)
        {
            try
            {
                int readCount = _stream.EndRead(ar);
                if (readCount <= 0)
                {
                    ConnectionClosed?.Invoke();
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
                            int size = _packetPosition;
                            _packetPosition = 0;
                            _processLine(_packetBuffer.AsMemory(0, size));
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
                Debug.WriteLine("Exception in AsyncStreamReader.ReadCompleted: " + ex.Message);
                ConnectionClosed?.Invoke();
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            if (_readBuffer != null)
                _pool.Return(_readBuffer);
            if (_packetBuffer != null)
                _pool.Return(_packetBuffer);

            GC.SuppressFinalize(this);
        }
    }
}
