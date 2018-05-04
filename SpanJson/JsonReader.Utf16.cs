﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SpanJson.Formatters.Dynamic;
using SpanJson.Helpers;

namespace SpanJson
{
    public ref partial struct JsonReader<TSymbol> where TSymbol : struct
    {
        public sbyte ReadUtf16SByte()
        {
            return (sbyte) ReadUtf16NumberInt64();
        }

        public short ReadUtf16Int16()
        {
            return (short) ReadUtf16NumberInt64();
        }

        public int ReadUtf16Int32()
        {
            return (int) ReadUtf16NumberInt64();
        }

        public long ReadUtf16Int64()
        {
            return ReadUtf16NumberInt64();
        }

        public byte ReadUtf16Byte()
        {
            return (byte) ReadUtf16NumberUInt64();
        }

        public ushort ReadUtf16UInt16()
        {
            return (ushort) ReadUtf16NumberUInt64();
        }

        public uint ReadUtf16UInt32()
        {
            return (uint) ReadUtf16NumberUInt64();
        }

        public ulong ReadUtf16UInt64()
        {
            return ReadUtf16NumberUInt64();
        }

        public float ReadUtf16Single()
        {
            return float.Parse(ReadUtf16NumberInternal(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public double ReadUtf16Double()
        {
            return double.Parse(ReadUtf16NumberInternal(), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadUtf16NumberInternal()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (TryFindEndOfUtf16Number(pos, out var charsConsumed))
            {
                var result = _chars.Slice(pos, charsConsumed);
                pos += charsConsumed;
                return result;
            }

            ThrowJsonParserException(JsonParserException.ParserError.EndOfData);
            return null;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long ReadUtf16NumberInt64()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos >= _length)
            {
                ThrowJsonParserException(JsonParserException.ParserError.EndOfData);
                return default;
            }

            var firstChar = _chars[pos];
            var neg = false;
            if (firstChar == '-')
            {
                pos++;
                neg = true;
            }

            if (pos >= _length)
            {
                ThrowJsonParserException(JsonParserException.ParserError.EndOfData);
                return default;
            }

            var result = _chars[pos++] - 48L;
            uint value;
            while (pos < _length && (value = _chars[pos] - 48U) <= 9)
            {
                result = unchecked(result * 10 + value);
                pos++;
            }

            return neg ? unchecked(-result) : result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong ReadUtf16NumberUInt64()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos >= _length)
            {
                ThrowJsonParserException(JsonParserException.ParserError.EndOfData);
                return default;
            }

            var firstChar = _chars[pos];
            if (firstChar == '-')
            {
                ThrowJsonParserException(JsonParserException.ParserError.InvalidNumberFormat);
                return default;
            }

            var result = _chars[pos++] - 48UL;
            if (result > 9)
            {
                ThrowJsonParserException(JsonParserException.ParserError.InvalidNumberFormat);
                return default;
            }

            uint value;
            while (pos < _length && (value = _chars[pos] - 48U) <= 9)
            {
                result = checked(result * 10 + value);
                pos++;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumericUtf16Symbol(char c)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '+':
                case '-':
                case '.':
                case 'E':
                case 'e':
                    return true;
            }

            return false;
        }

        public bool ReadUtf16Boolean()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (_chars[pos] == JsonConstant.True) // just peek the char
            {
                if (_chars[pos + 1] != 'r')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                if (_chars[pos + 2] != 'u')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                if (_chars[pos + 3] != 'e')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                pos += 4;
                return true;
            }

            if (_chars[pos] == JsonConstant.False) // just peek the char
            {
                if (_chars[pos + 1] != 'a')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                if (_chars[pos + 2] != 'l')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                if (_chars[pos + 3] != 's')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                if (_chars[pos + 4] != 'e')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
                }

                pos += 5;
                return false;
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(bool));
            return false;
        }

        public char ReadUtf16Char()
        {
            var span = ReadUtf16StringSpan();
            var pos = 0;
            return ReadUtf16CharInternal(span, ref pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char ReadUtf16CharInternal(ReadOnlySpan<char> span, ref int pos)
        {
            if (span.Length == 1)
            {
                return span[pos++];
            }

            if (span[pos] == JsonConstant.ReverseSolidus)
            {
                pos++;
                switch (span[pos++])
                {
                    case JsonConstant.DoubleQuote:
                        return JsonConstant.DoubleQuote;
                    case JsonConstant.ReverseSolidus:
                        return JsonConstant.ReverseSolidus;
                    case JsonConstant.Solidus:
                        return JsonConstant.Solidus;
                    case 'b':
                        return '\b';
                    case 'f':
                        return '\f';
                    case 'n':
                        return '\n';
                    case 'r':
                        return '\r';
                    case 't':
                        return '\t';
                    case 'U':
                    case 'u':
                        if (span.Length == 6)
                        {
                            var result = (char) int.Parse(span.Slice(2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                            pos += 4;
                            return result;
                        }

                        break;
                }
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(char));
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUtf16EndObjectOrThrow()
        {
            if (!ReadUtf16IsEndObject())
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedEndObject);
            }
        }

        public DateTime ReadUtf16DateTime()
        {
            var span = ReadUtf16StringSpan();
            if (DateTimeParser.TryParseDateTime(span, out var value, out var charsConsumed))
            {
                return value;
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(DateTime));
            return default;
        }

        public DateTimeOffset ReadUtf16DateTimeOffset()
        {
            var span = ReadUtf16StringSpan();
            if (DateTimeParser.TryParseDateTimeOffset(span, out var value, out var charsConsumed))
            {
                return value;
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(DateTimeOffset));
            return default;
        }

        public TimeSpan ReadUtf16TimeSpan()
        {
            var span = ReadUtf16StringSpan();
            if (TimeSpan.TryParse(span, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(TimeSpan));
            return default;
        }

        public Guid ReadUtf16Guid()
        {
            var span = ReadUtf16StringSpan();
            if (Guid.TryParse(span, out var result))
            {
                return result;
            }

            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol, typeof(Guid));
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> ReadUtf16NameSpan()
        {
            var span = ReadUtf16StringSpan();
            if (_chars[_pos++] != JsonConstant.NameSeparator)
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            }

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf16EscapedName()
        {
            var span = ReadUtf16StringSpanInternal(out var escapedCharsSize);
            if (_chars[_pos++] != JsonConstant.NameSeparator)
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            }

            return escapedCharsSize == 0 ? span.ToString() : UnescapeUtf16(span, escapedCharsSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf16String()
        {
            if (ReadUtf16IsNull())
            {
                return null;
            }

            var span = ReadUtf16StringSpanInternal(out var escapedCharSize);
            return escapedCharSize == 0 ? span.ToString() : UnescapeUtf16(span, escapedCharSize);
        }

        private string UnescapeUtf16(ReadOnlySpan<char> span, int escapedCharSize)
        {
            var unescapedLength = span.Length - escapedCharSize;
            var result = new string('\0', unescapedLength);
            ref var c = ref MemoryMarshal.GetReference(result.AsSpan());
            var unescapedIndex = 0;
            var index = 0;
            while (index < span.Length)
            {
                var current = span[index++];
                if (current == JsonConstant.ReverseSolidus)
                {
                    current = span[index++];
                    switch (current)
                    {
                        case JsonConstant.DoubleQuote:
                            current = JsonConstant.DoubleQuote;
                            break;
                        case JsonConstant.ReverseSolidus:
                            current = JsonConstant.ReverseSolidus;
                            break;
                        case JsonConstant.Solidus:
                            current = JsonConstant.Solidus;
                            break;
                        case 'b':
                            current = '\b';
                            break;
                        case 'f':
                            current = '\f';
                            break;
                        case 'n':
                            current = '\n';
                            break;
                        case 'r':
                            current = '\r';
                            break;
                        case 't':
                            current = '\t';
                            break;
                        case 'U':
                        case 'u':
                        {
                            current = (char) int.Parse(span.Slice(index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                            index += 4;
                            break;
                        }

                        default:
                            ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol);
                            break;
                    }
                }

                Unsafe.Add(ref c, unescapedIndex++) = current;
            }

            return result;
        }


        /// <summary>
        ///     Not escaped
        /// </summary>
        public ReadOnlySpan<char> ReadUtf16StringSpan()
        {
            if (ReadUtf16IsNull())
            {
                return JsonConstant.NullTerminatorUtf16;
            }

            return ReadUtf16StringSpanInternal(out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadUtf16StringSpanInternal(out int escapedCharsSize)
        {
            ref var pos = ref _pos;
            if (_chars[pos] != JsonConstant.String)
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            }

            pos++;
            // We should also get info about how many escaped chars exist from here
            if (TryFindEndOfUtf16String(pos, out var charsConsumed, out escapedCharsSize))
            {
                var result = _chars.Slice(pos, charsConsumed);
                pos += charsConsumed + 1; // skip the JsonConstant.DoubleQuote too
                return result;
            }

            ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            return null;
        }

        /// <summary>
        ///     Includes the quotes on each end
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<char> ReadUtf16StringSpanWithQuotes(out int escapedCharsSize)
        {
            ref var pos = ref _pos;
            if (_chars[pos] != JsonConstant.String)
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            }

            // We should also get info about how many escaped chars exist from here
            if (TryFindEndOfUtf16String(pos + 1, out var charsConsumed, out escapedCharsSize))
            {
                var result = _chars.Slice(pos, charsConsumed + 2); // we include quotes in this version
                pos += charsConsumed + 2; // include both JsonConstant.DoubleQuote too 
                return result;
            }

            ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
            return null;
        }

        public decimal ReadUtf16Decimal()
        {
            return decimal.Parse(ReadUtf16NumberInternal(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUtf16IsNull()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos < _length && _chars[pos] == JsonConstant.Null) // just peek the char
            {
                if (_chars[pos + 1] != 'u')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol);
                }

                if (_chars[pos + 2] != 'l')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol);
                }

                if (_chars[pos + 3] != 'l')
                {
                    ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol);
                }

                pos += 4;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUtf16Null()
        {
            if (!ReadUtf16IsNull())
            {
                ThrowJsonParserException(JsonParserException.ParserError.InvalidSymbol);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SkipWhitespaceUtf16()
        {
            ref var pos = ref _pos;
            while (pos < _length)
            {
                var c = _chars[pos];
                switch (c)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                    {
                        pos++;
                        continue;
                    }
                    default:
                        return;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUtf16BeginArrayOrThrow()
        {
            if (!ReadUtf16BeginArray())
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedBeginArray);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUtf16BeginArray()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos < _length && _chars[pos] == JsonConstant.BeginArray)
            {
                pos++;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUtf16IsEndArrayOrValueSeparator(ref int count)
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos < _length && _chars[pos] == JsonConstant.EndArray)
            {
                pos++;
                return true;
            }

            if (count++ > 0)
            {
                if (pos < _length && _chars[pos] == JsonConstant.ValueSeparator)
                {
                    pos++;
                    return false;
                }

                ThrowJsonParserException(JsonParserException.ParserError.ExpectedSeparator);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUtf16IsBeginObject()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (_chars[pos] == JsonConstant.BeginObject)
            {
                pos++;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadUtf16BeginObjectOrThrow()
        {
            if (!ReadUtf16IsBeginObject())
            {
                ThrowJsonParserException(JsonParserException.ParserError.ExpectedBeginObject);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUtf16IsEndObject()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (_chars[pos] == JsonConstant.EndObject)
            {
                pos++;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUtf16IsEndObjectOrValueSeparator(ref int count)
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos < _length && _chars[pos] == JsonConstant.EndObject)
            {
                pos++;
                return true;
            }

            if (count++ > 0)
            {
                if (_chars[pos] == JsonConstant.ValueSeparator)
                {
                    pos++;
                    return false;
                }

                ThrowJsonParserException(JsonParserException.ParserError.ExpectedSeparator);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Version ReadUtf16Version()
        {
            var stringValue = ReadUtf16String();
            if (stringValue == null)
            {
                return default;
            }

            return Version.Parse(stringValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Uri ReadUtf16Uri()
        {
            var stringValue = ReadUtf16String();
            if (stringValue == null)
            {
                return default;
            }

            return new Uri(stringValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipNextUtf16Segment()
        {
            SkipNextUtf16Segment(0);
        }

        private void SkipNextUtf16Segment(int stack)
        {
            ref var pos = ref _pos;
            var token = ReadUtf16NextToken();
            switch (token)
            {
                case JsonToken.None:
                    break;
                case JsonToken.BeginArray:
                case JsonToken.BeginObject:
                {
                    pos++;
                    SkipNextUtf16Segment(stack + 1);
                    break;
                }
                case JsonToken.EndObject:
                case JsonToken.EndArray:
                {
                    pos++;
                    if (stack - 1 > 0)
                    {
                        SkipNextUtf16Segment(stack - 1);
                    }

                    break;
                }
                case JsonToken.Number:
                case JsonToken.String:
                case JsonToken.True:
                case JsonToken.False:
                case JsonToken.Null:
                case JsonToken.ValueSeparator:
                case JsonToken.NameSeparator:
                {
                    do
                    {
                        SkipNextUtf16Value(token);
                        token = ReadUtf16NextToken();
                    } while (stack > 0 && (byte) token > 4); // No None or the Begin/End-Array/Object tokens

                    if (stack > 0)
                    {
                        SkipNextUtf16Segment(stack);
                    }

                    break;
                }
            }
        }

        private void SkipNextUtf16Value(JsonToken token)
        {
            ref var pos = ref _pos;
            switch (token)
            {
                case JsonToken.None:
                    break;
                case JsonToken.BeginObject:
                case JsonToken.EndObject:
                case JsonToken.BeginArray:
                case JsonToken.EndArray:
                case JsonToken.ValueSeparator:
                case JsonToken.NameSeparator:
                    pos++;
                    break;
                case JsonToken.Number:
                {
                    if (TryFindEndOfUtf16Number(pos, out var charsConsumed))
                    {
                        pos += charsConsumed;
                    }

                    break;
                }
                case JsonToken.String:
                {
                    pos++;
                    if (TryFindEndOfUtf16String(pos, out var charsConsumed, out _))
                    {
                        pos += charsConsumed + 1; // skip JsonConstant.DoubleQuote too
                        return;
                    }

                    ThrowJsonParserException(JsonParserException.ParserError.ExpectedDoubleQuote);
                    break;
                }
                case JsonToken.Null:
                case JsonToken.True:
                    pos += 4;
                    break;
                case JsonToken.False:
                    pos += 5;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindEndOfUtf16Number(int pos, out int charsConsumed)
        {
            var i = pos;
            var length = _chars.Length;
            for (; i < length; i++)
            {
                var c = _chars[i];
                if (!IsNumericUtf16Symbol(c))
                {
                    break;
                }
            }

            if (i > pos)
            {
                charsConsumed = i - pos;
                return true;
            }

            charsConsumed = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryFindEndOfUtf16String(int pos, out int charsConsumed, out int escapedCharsSize)
        {
            escapedCharsSize = 0;
            for (var i = pos; i < _chars.Length; i++)
            {
                var c = _chars[i];
                if (c == JsonConstant.ReverseSolidus)
                {
                    escapedCharsSize++;
                    i++;
                    var nextChar = _chars[i]; // check what type of escaped char it is
                    if (nextChar == 'u' || nextChar == 'U')
                    {
                        escapedCharsSize += 4; // add only 4 and not 5 as we still need one unescaped char
                        i += 4;
                    }

                }
                else if (c == JsonConstant.String)
                {
                    charsConsumed = i - pos;
                    return true;
                }
            }

            charsConsumed = default;
            return false;
        }

        public object ReadUtf16Dynamic()
        {
            return ReadUtf16Dynamic(0);
        }

        public object ReadUtf16Dynamic(int stack)
        {
            ref var pos = ref _pos;
            var nextToken = ReadUtf16NextToken();
            if (stack > 256)
            {
                ThrowJsonParserException(JsonParserException.ParserError.NestingTooDeep);
            }

            switch (nextToken)
            {
                case JsonToken.Null:
                {
                    ReadUtf16Null();
                    return null;
                }
                case JsonToken.False:
                case JsonToken.True:
                {
                    return ReadUtf16Boolean();
                }
                case JsonToken.Number:
                {
                    return new SpanJsonDynamicUtf16Number(ReadUtf16NumberInternal());
                }
                case JsonToken.String:
                {
                    var span = ReadUtf16StringSpanWithQuotes(out _);
                    return new SpanJsonDynamicUtf16String(span);
                }
                case JsonToken.BeginObject:
                {
                    pos++;
                    var count = 0;
                    var dictionary = new Dictionary<string, object>();
                    while (!TryReadUtf16IsEndObjectOrValueSeparator(ref count))
                    {
                        var name = ReadUtf16NameSpan().ToString();
                        var value = ReadUtf16Dynamic(stack + 1);
                        dictionary[name] = value; // take last one
                    }

                    return new SpanJsonDynamicObject(dictionary);
                }
                case JsonToken.BeginArray:
                {
                    pos++;
                    var count = 0;
                    var list = new List<object>();
                    while (!TryReadUtf16IsEndArrayOrValueSeparator(ref count))
                    {
                        var value = ReadUtf16Dynamic(stack + 1);
                        list.Add(value);
                    }

                    return new SpanJsonDynamicArray<TSymbol>(list.ToArray());
                }
                default:
                {
                    ThrowJsonParserException(JsonParserException.ParserError.EndOfData);
                    return default;
                }
            }
        }

        private JsonToken ReadUtf16NextToken()
        {
            SkipWhitespaceUtf16();
            ref var pos = ref _pos;
            if (pos >= _chars.Length)
            {
                return JsonToken.None;
            }

            var c = _chars[pos];
            switch (c)
            {
                case JsonConstant.BeginObject:
                    return JsonToken.BeginObject;
                case JsonConstant.EndObject:
                    return JsonToken.EndObject;
                case JsonConstant.BeginArray:
                    return JsonToken.BeginArray;
                case JsonConstant.EndArray:
                    return JsonToken.EndArray;
                case JsonConstant.String:
                    return JsonToken.String;
                case JsonConstant.True:
                    return JsonToken.True;
                case JsonConstant.False:
                    return JsonToken.False;
                case JsonConstant.Null:
                    return JsonToken.Null;
                case JsonConstant.ValueSeparator:
                    return JsonToken.ValueSeparator;
                case JsonConstant.NameSeparator:
                    return JsonToken.NameSeparator;
                case '+':
                case '-':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '0':
                    return JsonToken.Number;
                default:
                    return JsonToken.None;
            }
        }
    }
}