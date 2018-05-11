using System;
using System.Collections.Generic;
using System.IO.Pipelines;

namespace JsonRpcLib.Server
{
	public interface IJsonRpcServer : IDisposable
	{
		IReadOnlyList<IClient> Clients { get; }

		IClient AttachClient(string address, IDuplexPipe duplexPipe);
		void Bind(object handler, string prefix = "");
		void Bind(string method, Delegate call);
		void Bind(Type type, string prefix = "");
	}
}
