using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SpanJson.Formatters.Dynamic;
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

        public static bool ConvertToConcreteType(Type inputType, Type outputType, ref object value)
        {
            if(typeof(SpanJsonDynamic<byte>).IsAssignableFrom(inputType))
            {
                var v = (SpanJsonDynamic<byte>)value;
                return v.TryConvert(outputType, out value);
            }
            else if (inputType == typeof(SpanJsonDynamicArray<byte>))
            {
                var nt = (SpanJsonDynamicArray<byte>)value;
                var et = outputType.GetElementType();

                var a = Array.CreateInstance(et, nt.Length);
                int i = 0;
                foreach (SpanJsonDynamic<byte> ev in nt)
                {
                    if (!ev.TryConvert(et, out object v))
                        return false;
                    a.SetValue(v, i++);
                }

                value = a;
                return true;
            }
            return false;
        }
    }
}
