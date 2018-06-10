using System.Threading;
using System.Threading.Tasks;

namespace PerfTest
{
    static class Target
    {
        static int s_testCount;
        static TaskCompletionSource<int> s_completed = new TaskCompletionSource<int>();
        public static int Counter;

        public static Task PrepareNewTest(int count)
        {
            s_testCount = count;
            s_completed = new TaskCompletionSource<int>();
            Counter = 0;
            return s_completed.Task;
        }

        public static void SpeedNoArgs()
        {
            if (Interlocked.Increment(ref Counter) == s_testCount)
                s_completed.SetResult(Counter);
        }
    }
}
