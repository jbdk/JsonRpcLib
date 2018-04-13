using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using JsonRpcLib.Client;
using Xunit;

namespace Tests
{
    public class Connection
    {
        static int _nextPort = 43556;

        [Fact]
        public void CanConnect()
        {
            var port = Interlocked.Increment(ref _nextPort);
            using (var server = new TestServer(port))
            {
                using (var client = new TestClient(port))
                {
                    bool connected = server.ClientConnected.Wait(1000);
                    connected.Should().BeTrue();
                }
            }
        }

        [Fact]
        public void CanGetReply()
        {
            var port = Interlocked.Increment(ref _nextPort);
            using (var server = new TestServer(port))
            {
                using (var client = new TestClient(port))
                {
                    var reply = client.Invoke<TestServer.TestResponseData>("TestResponse", 1, "abc");

                    reply.Should().NotBeNull();
                    reply.Number.Should().Be(432);
                    reply.StringArray.Should().BeEquivalentTo("a", "b", "c", "d", "e");
                    reply.Text1.Should().Be("Some text");
                }
            }
        }

        [Fact]
        public void Notify_WithNoResponse()
        {
            var port = Interlocked.Increment(ref _nextPort);
            using (var server = new TestServer(port))
            {
                using (var client = new TestClient(port))
                {
                    client.Notify("TestResponse", 1, "abc");
                    int receivedCount = client.ReceiveDataWithin(50);

                    receivedCount.Should().Be(0);
                    server.LastMessageReceived.Should().NotBeNull();
                }
            }
        }
    }
}

