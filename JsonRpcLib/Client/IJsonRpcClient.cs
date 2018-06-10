using System;
using System.Threading.Tasks;

namespace JsonRpcLib.Client
{
    public interface IJsonRpcClient : IDisposable
    {
        TimeSpan Timeout { get; set; }

        void EnterCaptureMode(string initiateMethod, Func<string, bool> handler);
        void Flush();
        Task InvokeAsync(string method, params object[] args);
        Task<T> InvokeAsync<T>(string method, params object[] args);
        void Notify(string method, params object[] args);
    }
}
