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
    }
}
