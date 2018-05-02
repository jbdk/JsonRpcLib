using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.IO.Pipelines.Text.Primitives;
using System.Net;
using System.Threading.Tasks;

namespace JsonRpcLib
{
    internal class AsyncLineReader : IDisposable
    {
        private static readonly MemoryPool<byte> _pool = MemoryPool<byte>.Shared;

        private readonly PipeReader _reader;
        private readonly Action<RentedBuffer> _processLine;
        private readonly Task _readerThread;

        public Action ConnectionClosed { get; set; }

        public AsyncLineReader(PipeReader reader, Action<RentedBuffer> processLine)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _processLine = processLine ?? throw new ArgumentNullException(nameof(processLine));
            _readerThread = Task.Factory.StartNew(ReaderThread);
        }

        private async void ReaderThread()
        {
            while (true)
            {
                var result = await _reader.ReadAsync();
                var input = result.Buffer;

                try
                {
                    if (result.IsCompleted && result.Buffer.IsEmpty)
                    {
                        // No more data
                        ConnectionClosed?.Invoke();
                        break;
                    }

                    // Extract each line from the input
                    while (input.TrySliceTo((byte)'\n', out var slice, out var cursor))
                    {
                        input = input.Slice(cursor).Slice(1);

                        int size = (int)slice.Length;
                        var block = _pool.Rent(size);
                        slice.CopyTo(block.Memory.Span);

                        try
                        {
                            _processLine(new RentedBuffer(block, size));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing line \"{slice.GetUtf8Span()}\": {ex.Message}");
                        }
                    }
                }
                finally
                {
                    // // Consume the input
                    _reader.AdvanceTo(input.Start, input.End);
                }
            }

            //_reader.Complete();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

}
