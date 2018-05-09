using System.Threading;
using System.Threading.Tasks;

namespace PerfTest
{
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
