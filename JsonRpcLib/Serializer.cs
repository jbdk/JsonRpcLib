using System;
using System.IO;
using Utf8Json;
using Utf8Json.Resolvers;

namespace JsonRpcLib
{
    internal static class Serializer
    {
        public static string Serialize<T>(T data) where T : new()
        {
            return JsonSerializer.ToJsonString<T>(data, StandardResolver.AllowPrivateSnakeCase);
        }

        public static void Serialize<T>(T data, Stream stream) where T : new()
        {
            JsonSerializer.Serialize<T>(stream, data, StandardResolver.AllowPrivateSnakeCase);
        }

        public static T Deserialize<T>(string json) where T : new()
        {
            return JsonSerializer.Deserialize<T>(json, StandardResolver.AllowPrivateSnakeCase);
        }
    }
}
