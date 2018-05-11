using System;
using System.Diagnostics;
using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Text;
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
                var clients = new JsonRpcClient[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    clients[i] = MyClient.ConnectAsync(port).Result;
                }

                Console.WriteLine($"Warmup");
                RunLatencyTest(true).Wait();
                RunNotifyTest(threadCount, threadCount*10000, clients, true);
                RunInvokeTest(threadCount, threadCount*1000, clients, true);


                Console.WriteLine($"Running the tests...\n");
                RunLatencyTest().Wait();
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
                Task.Factory.StartNew(() => NotifyTest(client, testCount / threadCount), TaskCreationOptions.LongRunning);
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

            var tasks = new Task[threadCount];
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
                client.InvokeAsync("SpeedNoArgs").Wait();
            }
        }

#if true
        private static async Task RunLatencyTest(bool isWarmup = false)
        {
            int testCount = (isWarmup) ? 10 : 20_000;
            if (!isWarmup)
                Console.WriteLine($"Running 1 connection latency test ({testCount} iterations");
            const int port = 15435;
            byte[] buffer = Encoding.UTF8.GetBytes("SOME TEST DATA!\n");

            using (var server = new SimpleTcpServer(port))
            {
                using (var client = await SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port)))
                {
                    var sw = Stopwatch.StartNew();

                    for (int i = 0; i < testCount; i++)
                    {
                        await client.Output.WriteAsync(buffer);
                        await client.Output.FlushAsync();

                        var result = await client.Input.ReadAsync();
                        if (result.Buffer.Length != buffer.Length)
                        {
                            Console.WriteLine("LatencyTest for wrong response!");
                            break;
                        }
                        client.Input.AdvanceTo(result.Buffer.End);

                        if(!isWarmup && i>2000 && i%2000 == 0)
                        {
                            var t = sw.ElapsedMilliseconds;
                            Console.Write("  {1} r/s ({0}ms elapsed)    \r", t, (int)( (double)(i+1) / ( (double)t / 1000 ) ));
                        }
                    }

                    var t1 = sw.ElapsedMilliseconds;
                    if (!isWarmup)
                        Console.WriteLine("  {1} r/s ({0}ms elapsed)     ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
                }
            }
        }

#else
        private static async Task RunLatencyTest2(bool isWarmup = false)
        {
            int testCount = ( isWarmup ) ? 10 : 100_000;
            if (!isWarmup)
                Console.WriteLine($"Running 1 connection latency test ({testCount} iterations");
            const int port = 15435;
            byte[] buffer = Encoding.UTF8.GetBytes("SOME TEST DATA!\n");
            ArraySegment<byte> receive = new ArraySegment<byte>(new byte[100]);

            using (var server = new SimpleTcpServer(port))
            {
                using (var client = new TcpClient("127.0.0.1", port))
                {
                    client.Client.NoDelay = true;

                    var sw = Stopwatch.StartNew();

                    for (int i = 0; i < testCount; i++)
                    {
                        await client.Client.SendAsync(buffer, SocketFlags.None);

                        int len = await client.Client.ReceiveAsync(receive, SocketFlags.None);
                        if (len != buffer.Length)
                        {
                            Console.WriteLine("LatencyTest for wrong response!");
                            break;
                        }

                        if (!isWarmup && i > 2000 && i % 2000 == 0)
                        {
                            var t = sw.ElapsedMilliseconds;
                            Console.Write("  {1} r/s ({0}ms elapsed)    \r", t, (int)( (double)( i + 1 ) / ( (double)t / 1000 ) ));
                        }
                    }

                    var t1 = sw.ElapsedMilliseconds;
                    if (!isWarmup)
                        Console.WriteLine("  {1} r/s ({0}ms elapsed)     ", t1, (int)( (double)testCount / ( (double)t1 / 1000 ) ));
                }
            }
        }
#endif

    }
}
