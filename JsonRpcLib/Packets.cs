using System.Runtime.Serialization;

namespace JsonRpcLib
{
    [DataContract]
    internal class Notify
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "params", EmitDefaultValue = false)]
        public object[] Params { get; set; }
    }

    [DataContract]
    internal class Request : Notify
    {
        [DataMember(Name = "id")]
        public int Id { get; set; } = -1;
    }

    [DataContract]
    internal struct Error
    {
        [DataMember(Name = "code")]
        public int Code { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }

    [DataContract]
    internal struct Response
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "error", EmitDefaultValue = false)]
        public Error? Error { get; set; }
    }

    [DataContract]
    internal struct Response<T>
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; }

        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "error", EmitDefaultValue = false)]
        public Error? Error { get; set; }

        [DataMember(Name = "result")]
        public T Result { get; set; }
    }
}
