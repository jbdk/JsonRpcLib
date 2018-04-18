using System;

namespace JsonRpcLib
{
    public struct RentedBuffer
    {
        private readonly byte[] _buffer;
        private readonly int _length;
        private readonly Action<byte[]> _returnBuffer;

        public bool IsEmpty => _buffer == null;
        public Span<byte> Span => _buffer.AsSpan(0, _length);

        public RentedBuffer(byte[] buffer, int length, Action<byte[]> returnBuffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _length = length;
            _returnBuffer = returnBuffer ?? throw new ArgumentNullException(nameof(returnBuffer));
        }

        public void Return()
        {
            _returnBuffer?.Invoke(_buffer);
        }
    }
}
