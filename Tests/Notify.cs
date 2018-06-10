using System.Text;
using FluentAssertions;
using FluentAssertions.Json;
using JsonRpcLib.Server;
using Moq;
using Tests.Fakes;
using Xunit;

namespace Tests
{
    public class Notify : Base
    {
        [Fact]
        public void NoReturnOnNotify()
        {
            string reply = null;

            var handler = new TestHandlerFake();
            var server = new JsonRpcServer();
            var clientMock = new Mock<JsonRpcServer.ClientConnection>(1, "localhost", _fakePipe, _process, Encoding.UTF8);
            clientMock.Setup(x => x.WriteString(It.IsAny<string>())).Callback<string>(s => reply = s);

            server.Bind(handler);
            server.ExecuteHandler(clientMock.Object, -1, "ReturnStringArray", null);

            reply.Should().BeNull();
        }
    }
}
