﻿using System;
using System.Diagnostics;
using System.Net.Sockets;
using JsonRpcLib.Client;

namespace PerfTest
{
    public class MyClient : JsonRpcClient
    {
        public MyClient(int port) : base(new TcpClient("127.0.0.1", port) { NoDelay = true }.GetStream())
        {
            Timeout = Debugger.IsAttached ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(1);
        }
    }
}
