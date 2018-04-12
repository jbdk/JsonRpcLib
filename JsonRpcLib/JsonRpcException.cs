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
    }
}
