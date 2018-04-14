using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Fakes
{
    public class TestHandlerFake
    {
        public int FirstTest(int n, string s, bool b, object o)
        {
            return 23;
        }

        public void ByteTest(byte n)
        {
        }

        public void DefaultArgs(int n, bool b = true)
        {
        }

        public uint UIntTest(uint n)
        {
            return n;
        }

        public void DateTimeTest(DateTime time)
        {
        }

        public void TimeSpanTest(TimeSpan ts)
        {
        }

        public int StringArrayTest(string[] a)
        {
            return a.Length;
        }

        public int[] ReturnPrimitiveArray()
        {
            return new int[] { 1, 2, 3, 4, 5 };
        }

        public string[] ReturnStringArray()
        {
            return new string[] { "one", "two", "three" };
        }
    }
}
