using BenchmarkDotNet.Running;

namespace Benchmark
{
	class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<Serialization>();
            var summary = BenchmarkRunner.Run<ResponseTime>();
        }
    }
}
