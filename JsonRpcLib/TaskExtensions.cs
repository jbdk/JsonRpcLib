using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JsonRpcLib
{
    static public class TaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Forget(this Task task) { }
    }
}
