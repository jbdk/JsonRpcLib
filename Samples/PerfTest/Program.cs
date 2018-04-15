using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PerfTest
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple PerfTest\n");

            int threadCount = Debugger.IsAttached ? 1 : 8;
            const int testCount = 1_000_000;
            const int port = 54343;

            Console.WriteLine($"Using {threadCount} client threads");

            using (var server = new MyServer(port))
            {
                server.Bind<Target>();

                MyClient[] clients = new MyClient[threadCount];
                for (int i = 0; i < threadCount; i++)
                    clients[i] = new MyClient(port);

                RunNotifyTest(threadCount, testCount, clients);
                RunInvokeTest(threadCount, testCount / 10, clients);
            }
        }

        private static void RunNotifyTest(int threadCount, int testCount, MyClient[] clients)
        {
            var completed = Target.PrepareNewTest(testCount);
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"{testCount} Notify request to the server (static class handler, and no args)");

            for (int i = 0; i < threadCount; i++)
            {
                var client = clients[i];
                Task.Factory.StartNew(() => NotifyTest(client, testCount / threadCount), TaskCreationOptions.LongRunning);
            }

            while (!completed.Wait(100))
                Console.Write($"  {Target.Counter}\r");

            var t1 = sw.ElapsedMilliseconds;
            Console.WriteLine("  {1} r/s ({0}ms elapsed) ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
        }

        private static void RunInvokeTest(int threadCount, int testCount, MyClient[] clients)
        {
            var completed = Target.PrepareNewTest(testCount);
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"{testCount} Invoke request to the server (static class handler, and no args)");

            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var client = clients[i];
                tasks[i] = Task.Factory.StartNew(() => InvokeTest(client, testCount / threadCount), TaskCreationOptions.LongRunning);
            }

            while (!completed.Wait(100))
                Console.Write($"  {Target.Counter}\r");

            Task.WaitAll(tasks);

            var t1 = sw.ElapsedMilliseconds;
            Console.WriteLine("  {1} r/s ({0}ms elapsed) ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
        }

        private static void NotifyTest(MyClient client, int testCount)
        {
            for (int i = 0; i < testCount; i++)
            {
                client.Notify("SpeedNoArgs");
            }
        }

        private static void InvokeTest(MyClient client, int testCount)
        {
            for (int i = 0; i < testCount; i++)
            {
                client.Invoke("SpeedNoArgs");
            }
        }
    }

    class Target
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
