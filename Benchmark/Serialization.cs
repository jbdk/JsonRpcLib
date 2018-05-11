using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using JsonRpcLib;

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
            Server.Bind(typeof(Handlers));
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
            Client.InvokeAsync("FunctionOne");
        }

        [Benchmark]
        public void Notify_FunctionOne()
        {
            Client.Notify("FunctionOne");
        }
    }
}
