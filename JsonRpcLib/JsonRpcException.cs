using System;

namespace JsonRpcLib
{
    public sealed class JsonRpcException : Exception
    {
        public int Code { get; set; }

        public JsonRpcException(string message) : base(message)
        {
        }

        internal JsonRpcException(Error error) : base(error.Message)
        {
            Code = error.Code;
        }

        public JsonRpcException() : base()
        {
        }

        private JsonRpcException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        public JsonRpcException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
