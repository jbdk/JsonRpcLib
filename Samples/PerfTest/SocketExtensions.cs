using System.IO.Pipelines.Networking.Sockets;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace PerfTest
{
    public static class SocketExtensions
    {
        static readonly PropertyInfo s_socketProperty;

        static SocketExtensions()
        {
            s_socketProperty = typeof(SocketConnection).GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// Get the remote IPv4 address of the SocketConnection.
        /// WARNING: Uses reflection to access the private Socket property because there is currently no public way
        /// to get this information!
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <returns>IPv4 address as string or null if unknown</returns>
        public static string GetRemoteIp(this SocketConnection conn)
        {
            if (s_socketProperty?.GetValue(conn) is Socket socket)
            {
                var ipEP = socket.RemoteEndPoint as IPEndPoint;
                return ipEP.Address.MapToIPv4().ToString();
            }
            return null;
        }
    }
}
