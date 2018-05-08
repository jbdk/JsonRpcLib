using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SpanJson.JsonSerializer.Generic;

namespace JsonRpcLib
{
    internal static class Serializer
    {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Serialize<T>(T input) where T : new()
        {
            return Utf8.Serialize<T>(input);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Serialize<T>(Stream stream, T input)
        {
			Utf8.SerializeAsync<T>(input, stream).GetAwaiter().GetResult();
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Deserialize<T>(string json) where T : new()
        {
			return Utf8.Deserialize<T>(MemoryMarshal.AsBytes(json.AsSpan()));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Deserialize<T>(Span<byte> span)
        {
			return Utf8.Deserialize<T>(span);
		}
	}
}
