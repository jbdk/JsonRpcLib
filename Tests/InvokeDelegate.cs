using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
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
    }
}
