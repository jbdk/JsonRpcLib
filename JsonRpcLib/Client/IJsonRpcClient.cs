using System;
using System.Threading.Tasks;

namespace JsonRpcLib.Client
{
	public interface IJsonRpcClient : IDisposable
	{
		TimeSpan Timeout { get; set; }

		void EnterCaptureMode(string initiateMethod, Func<string, bool> handler);
		void Flush();
		Task Invoke(string method, params object[] args);
		Task<T> Invoke<T>(string method, params object[] args);
		void Notify(string method, params object[] args);
	}
}
