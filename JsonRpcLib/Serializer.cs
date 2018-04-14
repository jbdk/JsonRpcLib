using System;
using System.IO;
using Utf8Json;
using Utf8Json.Resolvers;

namespace JsonRpcLib
{
    internal static class Serializer
    {
        public readonly static IJsonFormatterResolver Resolver = StandardResolver.AllowPrivateSnakeCase;

        public static string Serialize<T>(T data) where T : new()
        {
            return JsonSerializer.ToJsonString<T>(data, Resolver);
        }

        public static void Serialize<T>(Stream stream, T data)
        {
            JsonSerializer.Serialize<T>(stream, data, Resolver);
        }

        public static T Deserialize<T>(string json) where T : new()
        {
            return JsonSerializer.Deserialize<T>(json, Resolver);
        }

        public static T Deserialize<T>(Span<byte> span)
        {
            return JsonSerializer.Deserialize<T>(span.ToArray(), Resolver);
        }
    }
}
