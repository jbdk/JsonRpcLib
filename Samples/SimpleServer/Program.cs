using System;
using System.Net;
using System.Net.Sockets;
using JsonRpcLib.Server;

namespace SimpleServer
{
    class Program
    {
        static TcpListener _listener;
        static JsonRpcServer _server;

        static void Main(string[] args)
        {
            Console.WriteLine("JSON RPC SimpleServer\n");

            _server = new JsonRpcServer();

            _listener = new TcpListener(IPAddress.Any, 11000);
            _listener.Start();

            // Accept first client connection
            _listener.BeginAcceptTcpClient(AcceptClient, null);

            Console.WriteLine($"Listening on port 11000");
            Console.WriteLine("Press ENTER to stop\n");
            Console.ReadLine();

            _listener.Stop();
            _server.Dispose();
        }

        private static void AcceptClient(IAsyncResult ar)
        {
            try
            {
                // Accept client and get remote ip-address
                TcpClient client = _listener.EndAcceptTcpClient(ar);
                IPAddress ipAddress = ( (IPEndPoint)client.Client.RemoteEndPoint ).Address;

                // Add client to server
                _server.AttachClient(ipAddress.ToString(), client.GetStream());

                Console.WriteLine($"Client from {ipAddress} connected");

                // Accept next client connection
                _listener.BeginAcceptTcpClient(AcceptClient, null);
            }
            catch (ObjectDisposedException)
            {
                // NOP
            }
        }
    }
}
