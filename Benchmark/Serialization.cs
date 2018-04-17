using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using JsonRpcLib;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Benchmark
{
    public abstract class Handlers
    {
        public static void FunctionOne()
        {
        }
    }

    [CoreJob]
    [MemoryDiagnoser]
    public class Serialization
    {
        const int PORT = 54568;
        readonly private static MyServer Server;
        readonly static Request ReqObject = new Request { Id = 1, JsonRpc = "2.0", Method = "test", Params = new object[] { 54 } };

        private MyClient Client;

        static Serialization()
        {
            Server = new MyServer(PORT);
            Server.Bind<Handlers>();
        }

        [GlobalSetup]
        public void Setup()
        {
            Client = new MyClient(PORT);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Client.Dispose();
        }

        //[Benchmark]
        //public ArraySegment<byte> Utf8Json_ArraySegment() => JsonSerializer.SerializeUnsafe(ReqObject, StandardResolver.AllowPrivate);

        [Benchmark]
        public void Invoke_FunctionOne()
        {
            Client.Invoke("FunctionOne");
        }

        [Benchmark]
        public void Notify_FunctionOne()
        {
            Client.Notify("FunctionOne");
        }
    }
}
