using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JsonRpcLib
{
    public class RingBuffer<T> : IDisposable where T : struct, IEquatable<T>
    {
        private readonly T[] _data;
        private int _inPos;
        private int _outPos;

        Encoding _encoding = Encoding.UTF8;

        public RingBuffer(int size)
        {
            _data = ArrayPool<T>.Shared.Rent(size);
        }

        public int Available
        {
            get {
                if (_inPos >= _outPos)
                    return _inPos - _outPos;
                else
                    return ( _data.Length - _outPos ) + _inPos;
            }
        }

        public void Clear()
        {
            _inPos = _outPos = 0;
        }

        public void Put(T e)
        {
            _data[_inPos] = e;
            _inPos = ( _inPos + 1 ) % _data.Length;
            if (_inPos == _outPos)
            {
                Debug.Fail("RingBuffer owerflow");
                _inPos = _outPos = 0;
            }
        }

        public void Put(ReadOnlySpan<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                _data[_inPos] = span[i];
                _inPos = ( _inPos + 1 ) % _data.Length;
                if (_inPos == _outPos)
                {
                    Debug.Fail("RingBuffer owerflow");
                    _inPos = _outPos = 0;
                }
            }
        }

        public int Take(Span<T> buffer)
        {
            if (_inPos == _outPos)
                return 0;

            int consumed = 0;
            if (_inPos > _outPos)
            {
                var span = _data.AsSpan(_outPos);
                span.Slice(0, buffer.Length).CopyTo(buffer);
                consumed = Math.Min(span.Length, buffer.Length);
                _outPos = ( _outPos + consumed ) % _data.Length;
                return consumed;
            }
            else
            {
                var span = _data.AsSpan(_outPos);
                consumed = Math.Min(span.Length, buffer.Length);
                span.Slice(0, consumed).CopyTo(buffer);
                if (consumed == buffer.Length)
                    return consumed;
                if (_inPos > 0)
                {
                    buffer = buffer.Slice(consumed);
                    span = _data.AsSpan(0, Math.Min(buffer.Length, _inPos));
                    span.CopyTo(buffer);
                    consumed += _inPos;
                }
                _outPos = ( _outPos + consumed ) % _data.Length;
                return consumed;
            }
        }

        public int TakeUntil(Span<T> buffer, T stopAt)
        {
            int consumed = 0;
            while (_outPos != _inPos)
            {
                T e = _data[_outPos];
                if (EqualityComparer<T>.Default.Equals(e, stopAt))
                    break;
                buffer[consumed++] = e;
                _outPos = ( _outPos + 1 ) % _data.Length;
            }
            return consumed;
        }

        public void Skip(int amount)
        {
            _outPos = ( _outPos + Math.Min(amount, Available) ) % _data.Length;
        }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_data);
        }
    }
}
