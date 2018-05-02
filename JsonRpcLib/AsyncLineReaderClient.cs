using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace JsonRpcLib
{
    internal class AsyncLineReaderClient : IDisposable
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;
        private readonly PipeReader _reader;
        private readonly Action<RentedBuffer> _processLine;
        private readonly Task _readerThread;

        public Action ConnectionClosed { get; set; }

        public AsyncLineReaderClient(PipeReader reader, Action<RentedBuffer> processLine)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _processLine = processLine ?? throw new ArgumentNullException(nameof(processLine));
            _readerThread = Task.Factory.StartNew(ReaderThread, TaskCreationOptions.LongRunning);
        }

        private async void ReaderThread()
        {
            while(true)
            {
                var result = await _reader.ReadAsync();
                if(result.IsCompleted && result.Buffer.IsEmpty)
                {
                    Debug.WriteLine("Client connection was closed!");
                    break;
                }

                var inputBuffer = result.Buffer;
                var startPos = inputBuffer.GetPosition(0);
                int offset = 0;
                bool lastWasNewline = false;

                foreach (var buffer in inputBuffer)
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var b = buffer.Span[i];

                        if (b == '\n' || b == '\r')
                        {
                            if (lastWasNewline)
                                continue;
                            lastWasNewline = true;

                            var endPos = inputBuffer.GetPosition(offset);
                            var slice = inputBuffer.Slice(startPos, endPos);
                            int size = (int)slice.Length;
                            if (size > 0)
                            {
                                var block = _pool.Rent(size);
                                slice.CopyTo(block);
                                startPos = inputBuffer.GetPosition(offset + 1);

                                _processLine(new RentedBuffer(block, size, (mem) => _pool.Return(mem)));
                            }
                        }
                        else
                        {
                            lastWasNewline = false;
                        }

                        offset++;
                    }
                }
                _reader.AdvanceTo(startPos, inputBuffer.End);
            }

            _reader.Complete();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
