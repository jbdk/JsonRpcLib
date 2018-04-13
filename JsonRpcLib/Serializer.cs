using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;

namespace JsonRpcLib
{
    internal static class Serializer
    {
        public static string Serialize<T>(T data) where T : new()
        {
            return JsonConvert.SerializeObject(data, Formatting.None);
        }

        public static T Deserialize<T>(string json) where T : new()
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
