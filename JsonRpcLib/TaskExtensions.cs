using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace JsonRpcLib
{
    static public class TaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Forget(this Task task)
        {
            // Empty on purpose!
        }

        private const int DEFAULTTIMEOUT = 5000;

        public static Task OrTimeout(this Task task, int milliseconds = DEFAULTTIMEOUT) => OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds));

        public static async Task OrTimeout(this Task task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed != task)
            {
                throw new TimeoutException();
            }

            await task;
        }

        public static Task<T> OrTimeout<T>(this Task<T> task, int milliseconds = DEFAULTTIMEOUT) => OrTimeout(task, new TimeSpan(0, 0, 0, 0, milliseconds));

        public static async Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (completed != task)
            {
                throw new TimeoutException();
            }

            return await task;
        }
    }
}
