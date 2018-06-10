using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Json;
using JsonRpcLib;
using JsonRpcLib.Server;
using Moq;
using Xunit;

namespace Tests
{
    public class InvokeDelegate : Base
    {
        static int _nextPort = 34532;

        [Fact]
        public void CanInvokeDelegate()
        {
            Response reply = default;

            var fakePipe = new StreamDuplexPipe(PipeOptions.Default, new MemoryStream());
            var server = new JsonRpcServer();

            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", fakePipe, _process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            bool called = false;
            server.Bind("DelegateFunction", (Action)(() => { called = true; } ));
            server.ExecuteHandler(clientMock.Object, 43, "DelegateFunction", null);

            called.Should().BeTrue();
            reply.Should().NotBeNull();
            reply.Error.Should().BeNull();
            reply.Id.Should().Be(43);
        }

        [Fact]
        public void CanInvokeDelegate_WithArgs()
        {
            Response reply = default;

            var fakePipe = new StreamDuplexPipe(PipeOptions.Default, new MemoryStream());
            var server = new JsonRpcServer();

            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", fakePipe, _process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response)o);

            bool called = false;
            server.Bind("DelegateFunction", (Action<int, string>)( (a, b) => { called = b == "TestString"; } ));
            server.ExecuteHandler(clientMock.Object, 21, "DelegateFunction", new object[] { 176, "TestString" } );

            called.Should().BeTrue();
            reply.Should().NotBeNull();
            reply.Error.Should().BeNull();
            reply.Id.Should().Be(21);
        }

        [Fact]
        public void ClientInvokeDelegate()
        {
            var port = Interlocked.Increment(ref _nextPort);
            using (var server = new TestServer(port))
            {
                bool called = false;
                server.Bind("DelegateFunction", (Func<int, string, string>)( (a, b) =>
                {
                    called = b == "abc";
                    return b;
                } ));

                using (var client = new TestClient(port))
                {
                    var reply = client.InvokeAsync<string>("DelegateFunction", 1, "abc").Result;
                    reply.Should().Be("abc");
                }

                called.Should().BeTrue();
            }
        }

        [Fact]
        public void CanInvokeDelegate_WithArgs_ReturnValue()
        {
            Response<object> reply = default;

            var fakePipe = new StreamDuplexPipe(PipeOptions.Default, new MemoryStream());
            var server = new JsonRpcServer();

            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", fakePipe, _process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteAsJson(It.IsAny<object>())).Callback<object>(o => reply = (Response<object>)o);

            bool called = false;
            server.Bind("DelegateFunction", (Func<int, string, string>)( (a, b) =>
            {
                called = b == "TestString";
                return b;
            } ));
            server.ExecuteHandler(clientMock.Object, 21, "DelegateFunction", new object[] { 176, "TestString" });

            called.Should().BeTrue();
            reply.Should().NotBeNull();
            reply.Error.Should().BeNull();
            reply.Id.Should().Be(21);
            reply.Result.Should().NotBeNull();
            reply.Result.Should().Be("TestString");
        }
    }
}
