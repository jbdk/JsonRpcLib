using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JsonRpcLib.Client;

namespace PerfTest
{
    static class Program
    {
        private const int UPDATE_DELAY_IN_MS = 100;

        static void Main(string[] args)
        {
            Console.WriteLine("**** Simple PerfTest ****\n");

            int threadCount = Debugger.IsAttached ? 1 : 8;
            const int testCount = 1_000_000;
            const int port = 54343;

            // Make progress updates smoother
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;


            using (var server = new MyServer(port))
            {
                server.Bind(typeof(Target));    // Bind to functions on static class

                Console.WriteLine($"Making {threadCount} client connections");
                JsonRpcClient[] clients = new JsonRpcClient[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    clients[i] = MyClient.ConnectAsync(port).Result;
                }

                Console.WriteLine($"Warmup");
                RunNotifyTest(threadCount, threadCount*100, clients, true);
                RunInvokeTest(threadCount, threadCount*10, clients, true);


                Console.WriteLine($"Running the tests...\n");
                RunNotifyTest(threadCount, testCount, clients);
                RunInvokeTest(threadCount, testCount / 10, clients);
            }
        }

        private static void RunNotifyTest(int threadCount, int testCount, JsonRpcClient[] clients, bool isWarmup = false)
        {
            var completed = Target.PrepareNewTest(testCount);
            var sw = Stopwatch.StartNew();

            if(!isWarmup)
                Console.WriteLine($"{testCount} Notify request to the server (static class handler, and no args)");

            for (int i = 0; i < threadCount; i++)
            {
                var client = clients[i];
                Task.Factory.StartNew(() => NotifyTest(client, testCount / threadCount));
            }

            while (!completed.Wait(UPDATE_DELAY_IN_MS))
            {
                if(!isWarmup)
                    Console.Write($"  {Target.Counter}\r");
            }

            var t1 = sw.ElapsedMilliseconds;
            if (!isWarmup)
                Console.WriteLine("  {1} r/s ({0}ms elapsed) ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
        }

        private static void RunInvokeTest(int threadCount, int testCount, JsonRpcClient[] clients, bool isWarmup = false)
        {
            var completed = Target.PrepareNewTest(testCount);
            var sw = Stopwatch.StartNew();

            if (!isWarmup)
                Console.WriteLine($"{testCount} Invoke request to the server (static class handler, and no args)");

            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var client = clients[i];
                tasks[i] = Task.Factory.StartNew(() => InvokeTest(client, testCount / threadCount), TaskCreationOptions.LongRunning);
            }

            while (!Task.WhenAll(tasks).Wait(UPDATE_DELAY_IN_MS))
            {
                if (!isWarmup)
                    Console.Write($"  {Target.Counter}\r");
            }

            var t1 = sw.ElapsedMilliseconds;
            if (!isWarmup)
                Console.WriteLine("  {1} r/s ({0}ms elapsed) ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
        }

        private static void NotifyTest(JsonRpcClient client, int testCount)
        {
            for (int i = 0; i < testCount; i++)
            {
                client.Notify("SpeedNoArgs");
            }
        }

        private static void InvokeTest(JsonRpcClient client, int testCount)
        {
            for (int i = 0; i < testCount; i++)
            {
                client.Invoke("SpeedNoArgs");
                //Interlocked.Increment(ref Target.Counter);
            }
        }
    }

    static class Target
    {
        static int _testCount;
        static TaskCompletionSource<int> _completed = new TaskCompletionSource<int>();
        public static int Counter;

        public static Task PrepareNewTest(int count)
        {
            _testCount = count;
            _completed = new TaskCompletionSource<int>();
            Counter = 0;
            return _completed.Task;
        }

        public static void SpeedNoArgs()
        {
            if (Interlocked.Increment(ref Counter) == _testCount)
                _completed.SetResult(Counter);
        }
    }
}
