using System;
using System.Threading.Tasks;

namespace JsonRpcLib.Server
{
    public interface IClient : IDisposable
    {
        string Address { get; }
        int Id { get; }
        bool IsConnected { get; }

        bool WriteString(string data);
        void WriteAsJson(object value);
    }
}
